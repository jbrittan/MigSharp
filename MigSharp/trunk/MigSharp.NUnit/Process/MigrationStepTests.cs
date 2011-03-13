using System;
using System.Data;

using MigSharp.Process;
using MigSharp.Providers;

using NUnit.Framework;

using Rhino.Mocks;

namespace MigSharp.NUnit.Process
{
    [TestFixture, Category("smoke")]
    public class MigrationStepTests
    {
        private const string TableName = "New Table";
        private const string ProviderInvariantName = "providerName";
        private const string FirstCommandText = "1st command";
        private const string SecondCommandText = "2nd command";

        [Test]
        public void TestUpgrading()
        {
            TestMigrating(MigrationDirection.Up, provider => provider.Expect(p => p.CreateTable(TableName, null, null)).IgnoreArguments().Return(new[] { FirstCommandText, SecondCommandText }));
        }

        [Test]
        public void TestDowngrading()
        {
            TestMigrating(MigrationDirection.Down, provider => provider.Expect(p => p.DropTable(TableName)).Return(new[] { FirstCommandText, SecondCommandText }));
        }

        private static void TestMigrating(MigrationDirection direction, Action<IProvider> setupExpectationOnProvider)
        {
            TestMigrationMetadata metadata = new TestMigrationMetadata();

            TestMigration migration = new TestMigration();
            IProvider provider = MockRepository.GenerateMock<IProvider>();
            setupExpectationOnProvider(provider);

            IProviderMetadata providerMetadata = MockRepository.GenerateStub<IProviderMetadata>();

            IDbTransaction transaction = MockRepository.GenerateMock<IDbTransaction>();
            transaction.Expect(t => t.Commit());

            IDbConnection connection = MockRepository.GenerateMock<IDbConnection>();
            connection.Expect(c => c.State).Return(ConnectionState.Open).Repeat.Any();
            connection.Expect(c => c.BeginTransaction()).Return(transaction);

            IDbCommand firstCommand = MockRepository.GenerateMock<IDbCommand>();
            firstCommand.Expect(c => c.CommandTimeout).SetPropertyWithArgument(0);
            firstCommand.Expect(c => c.Transaction).SetPropertyWithArgument(transaction);
            firstCommand.Expect(c => c.CommandText).SetPropertyWithArgument(FirstCommandText);
            firstCommand.Expect(c => c.CommandText).Return(FirstCommandText);
            firstCommand.Expect(c => c.ExecuteNonQuery()).Return(0);
            connection.Expect(c => c.CreateCommand()).Return(firstCommand).Repeat.Once();

            IDbCommand secondCommand = MockRepository.GenerateMock<IDbCommand>();
            secondCommand.Expect(c => c.CommandTimeout).SetPropertyWithArgument(0);
            secondCommand.Expect(c => c.Transaction).SetPropertyWithArgument(transaction);
            secondCommand.Expect(c => c.CommandText).SetPropertyWithArgument(SecondCommandText);
            secondCommand.Expect(c => c.CommandText).Return(SecondCommandText);
            secondCommand.Expect(c => c.ExecuteNonQuery()).Return(0);
            connection.Expect(c => c.CreateCommand()).Return(secondCommand).Repeat.Once();

            connection.Expect(c => c.Dispose());
            IDbConnectionFactory connectionFactory = MockRepository.GenerateStub<IDbConnectionFactory>();
            connectionFactory.Expect(c => c.OpenConnection(null)).IgnoreArguments().Return(connection);
            MigrationStep step = new MigrationStep(migration, metadata, direction, new ConnectionInfo("", ProviderInvariantName, true), provider, providerMetadata, connectionFactory);

            IVersioning versioning = MockRepository.GenerateMock<IVersioning>();
            versioning.Expect(v => v.Update(metadata, connection, transaction, direction));
#if DEBUG
            versioning.Expect(v => v.IsContained(metadata)).Return(direction == MigrationDirection.Up);
#endif
            step.Execute(versioning);

            connection.VerifyAllExpectations();
            transaction.VerifyAllExpectations();
            provider.VerifyAllExpectations();
            firstCommand.VerifyAllExpectations();
            secondCommand.VerifyAllExpectations();
            versioning.VerifyAllExpectations();
        }

        private class TestMigration : IReversibleMigration
        {
            public void Up(IDatabase db)
            {
                db.CreateTable(TableName)
                    .WithPrimaryKeyColumn("Id", DbType.Int32);
            }

            public void Down(IDatabase db)
            {
                db.Tables[TableName].Drop();
            }
        }

        private class TestMigrationMetadata : IMigrationMetadata
        {
            public string Tag { get { return null; } }
            public string ModuleName { get { return string.Empty; } }
            public long Timestamp { get { return 1; } }
        }
    }
}