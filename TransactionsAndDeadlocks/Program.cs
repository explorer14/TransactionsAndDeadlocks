using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Transactions;
using MySql.Data.MySqlClient;

namespace TransactionsAndDeadlocks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var repo = new Repository();
            var id = 1000;

            Parallel.For(0, 500,
                async index =>
                {
                    var product = new Product
                    {
                        Id = id,
                        Stock = index+1
                    };
                    
                    using (var scope = new TransactionScope(                        
                        TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await repo.Save(product);
                        scope.Complete();
                    }        
                });
            
            Console.Read();
        }
    }

    public class Product
    {
        public int Id { get; set; }

        public int Stock { get; set; }
    }

    public class Repository
    {
        public async Task Save(Product product)
        {
            using (var connection = new MySqlConnection("Server=127.0.0.1;Database=TrxDb;Uid=root;Password=pass123"))
            {
                await connection.OpenAsync();

                // This code deadlocks
                if (await Exists(product, connection))
                    await Update(product, connection);
                else
                    await Insert(product, connection);

                // This code doesn't
                //await Update(product, connection);
            }
        }

        private async Task Insert(Product product, MySqlConnection mySqlConnection)
        {
            var sql = "insert into TrxDb.Products values(@id, @stock,@version)";
            var cmd = mySqlConnection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@id", product.Id);
            cmd.Parameters.AddWithValue("@stock", product.Stock);
            cmd.Parameters.AddWithValue("@version", 1);

            var noOfRows = (long) await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"Inserted {noOfRows} row(s)");
        }

        private async Task Update(Product product, MySqlConnection mySqlConnection)
        {
            var sql = "update TrxDb.Products set stock = @stock where Id = @id and Version = 1";
            var cmd = mySqlConnection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@id", product.Id);
            cmd.Parameters.AddWithValue("@stock", product.Stock);

            var noOfRows = (long) await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"Updated {noOfRows} row(s)");
        }

        private async Task<bool> Exists(Product product, MySqlConnection mySqlConnection)
        {
            var sql = "select count(1) from TrxDb.Products where Id = @id";
            var cmd = mySqlConnection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@id", product.Id);

            var noOfRows = (long) await cmd.ExecuteScalarAsync();

            return noOfRows > 0;
        }
    }
}