﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace pluginAzureSqlServer.Infrastructure
{
    public class SqlClientDbProvider : IDbProvider
    {
        public bool TableExists(IDbTransaction tx, string schema, string table)
        {
            using (var cmd = tx.Connection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"SELECT COUNT(*) FROM [INFORMATION_SCHEMA].[TABLES]
                    WHERE [TABLE_SCHEMA] = @schema AND [TABLE_NAME] = @table";

                var schemaParam = cmd.CreateParameter();
                schemaParam.ParameterName = "@schema";
                schemaParam.Value = schema;
                cmd.Parameters.Add(schemaParam);

                var tableParam = cmd.CreateParameter();
                tableParam.ParameterName = "@table";
                tableParam.Value = table;
                cmd.Parameters.Add(tableParam);

                var count = Convert.ToInt64(cmd.ExecuteScalar());
                return (count > 0);
            }
        }

        public void WriteRow(IDbTransaction tx, string schema,
            string table, IEnumerable<FieldValue> values)
        {
            EnsureValidIdentifier(schema);
            EnsureValidIdentifier(table);

            using (var cmd = tx.Connection.CreateCommand())
            {
                var fieldsListBuilder = new StringBuilder();
                var paramsListBuilder = new StringBuilder();
                var paramCount = 0;
                foreach (var value in values)
                {
                    EnsureValidIdentifier(value.Field);

                    if (paramCount > 0)
                    {
                        fieldsListBuilder.Append(", ");
                        paramsListBuilder.Append(", ");
                    }

                    // Adding to fields list.
                    fieldsListBuilder.Append("[");
                    fieldsListBuilder.Append(value.Field);
                    fieldsListBuilder.Append("]");

                    // Adding to params list.
                    paramsListBuilder.Append("@param_");
                    paramsListBuilder.Append(paramCount);

                    ++paramCount;
                }                

                cmd.Transaction = tx;
                cmd.CommandText = string.Format(
                    "INSERT INTO [{0}].[{1}] ({2}) VALUES ({3})",
                    schema, table, fieldsListBuilder.ToString(), paramsListBuilder.ToString()
                    );

                var i = 0;
                foreach (var value in values)
                {
                    var valueParam = cmd.CreateParameter();
                    valueParam.ParameterName = "@param_" + i.ToString();
                    valueParam.Value = value.Value;
                    cmd.Parameters.Add(valueParam);
                }

                cmd.ExecuteNonQuery();
            }            
        }

        /// <summary>
        /// Ensure that string is a valid SQL-identifier.
        /// </summary>
        private void EnsureValidIdentifier(string id)
        {
            var valid = !string.IsNullOrEmpty(id) && !id.Contains("[") && !id.Contains("]");
            if (!valid)
            {
                throw new Exception(string.Format("Invalid identifier \"{0}\"", id));
            }
        }
    }
}