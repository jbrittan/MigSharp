﻿using MigSharp.Core;
using MigSharp.Core.Commands;

namespace MigSharp
{
    public class Database
    {
        private readonly MigrateCommand _root = new MigrateCommand();
        private readonly TableCollection _tables;

        internal ICommand Root { get { return _root; } }
        public IExistingTableCollection Tables { get { return _tables; } }

        public Database()
        {
            _tables = new TableCollection(_root);
        }

        public NewTable CreateTable(string tableName)
        {
            CreateTableCommand createTableCommand = new CreateTableCommand();
            _root.Add(createTableCommand);
            NewTable table = new NewTable(tableName, createTableCommand);
            return table;
        }
    }
}