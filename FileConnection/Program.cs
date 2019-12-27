using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileConnection
{
    class Program
    {
        static void Main(string[] args)
        {
            Test();
            Console.ReadKey();
        }

        static async void Test()
        {
            using (var fileConnection = new FileConnection("template.db"))
            {
                var nodeConnection = await fileConnection.ConnectNodeAsync("Test");
                var dataConnection = await nodeConnection.ConnectDataAsync("Image");
                using (var inputStream = File.OpenRead("1.jpg"))
                {
                    using (var stream = dataConnection.OpenWrite(inputStream.Length))
                    {
                        await inputStream.CopyToAsync(stream);
                    }
                }
                using (var stream = dataConnection.OpenRead())
                {
                    using (var outputStream = File.OpenWrite("2.jpg"))
                    {
                        await stream.CopyToAsync(outputStream);
                    }
                }
                await dataConnection.SetParameterAsync("Name", "Test");
                Console.WriteLine(await dataConnection.GetParameterAsync("Name"));
            }
        }
    }

    public static class Extensions
    {
        public static bool IsEmpty(this string str)
        {
            return str.Length == 0;
        }

        public static bool IsNotEmpty(this string str)
        {
            return str.Length != 0;
        }

        public static async Task WriteAsync(this IDataConnection dataConnection, byte[] bytes)
        {
            using(var stream = dataConnection.OpenWrite(bytes.Length))
            {
                await stream.WriteAsync(bytes);
            }
        }

        public static async Task<byte[]> ReadAsync(this IDataConnection dataConnection)
        {
            using (var stream = dataConnection.OpenRead())
            {
                var bytes = new byte[stream.Length];
                await stream.ReadAsync(bytes);
                return bytes;
            }
        }
    }

    public sealed class Name
    {
        public Name(string name)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var c in name)
            {
                if (Ignored.Contains(c))
                {
                    continue;
                }
                if (c == Separator)
                {
                    if (stringBuilder.Length > 0)
                    {
                        FirstName = stringBuilder.ToString();
                        Name childName = name.Substring(stringBuilder.Length + 1);
                        if (childName.FirstName.IsNotEmpty())
                        {
                            ChildName = childName;
                        }
                        return;
                    }
                    continue;
                }
                stringBuilder.Append(c);
            }
            if (stringBuilder.Length > 0)
            {
                FirstName = stringBuilder.ToString();
                return;
            }
            FirstName = string.Empty;
        }

        public const string Ignored = " ";

        public const char Separator = '.';

        public string FirstName { get; }

        public Name? ChildName { get; }

        public bool HasActiveName => FirstName.IsNotEmpty();

        public static implicit operator Name(string name)
        {
            return new Name(name);
        }

        public static implicit operator string(Name name)
        {
            return name.FirstName;
        }
    }

    public interface IFileConnection : INodeConnection, IDisposable
    {
    }

    public interface INodeConnection
    {
        Task<INodeConnection> ConnectNodeAsync(string name);

        Task<IDataConnection> ConnectDataAsync(string name);

        Task<string[]> GetNodeNamesAsync();
    }

    public interface IDataConnection
    {
        Stream OpenWrite(long length);

        Stream OpenRead();

        Task<string> GetParameterAsync(string name);

        Task SetParameterAsync(string name, string value);
    }

    public sealed class Node
    {
        [Key]
        public long ID { get; set; }

        public string Name { get; set; }

        public DateTime LastWriteTime { get; set; }

        public Node? Parent { get; set; }

        public List<Node> ChildrenNodes { get; set; }

        public List<Leaf> ChildrenLeafs { get; set; }

        public List<NodeParameter> Parameters { get; set; }

        public Node()
        {
        }
    }

    public sealed class Leaf
    {
        [Key]
        public long ID { get; set; }

        public string Name { get; set; }

        public DateTime LastWriteTime { get; set; }

        public Node Parent { get; set; }

        public List<LeafParameter> Parameters { get; set; }

        public long? DataID { get; set; }

        public Leaf()
        {

        }
    }

    public sealed class Data
    {
        [Key]
        public long ID { get; set; }

        public byte[] Value { get; set; }

        public Data()
        {

        }
    }

    public abstract class ParameterBase
    {
        [Key]
        public long ID { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        protected ParameterBase()
        {

        }
    }

    public sealed class NodeParameter : ParameterBase
    {

    }

    public sealed class LeafParameter : ParameterBase
    {

    }

    public sealed class FileFormatContext : DbContext
    {
        public DbSet<Node> Nodes { get; set; }

        public DbSet<NodeParameter> NodeParameters { get; set; }

        public DbSet<Leaf> Leafs { get; set; }

        public DbSet<LeafParameter> LeafParameters { get; set; }

        public DbSet<Data> Datas { get; set; }

        public FileFormatContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source=template.db;Cache=shared");
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlite(Connection);
        //}

        public FileFormatContext(string path)
        {
            if (!File.Exists(path))
                throw new Exception("文件不存在");
            _connectionString = $"Data Source={path};Cache=shared";
            Connection = new SqliteConnection(_connectionString);
            Connection.Open();
        }

        private readonly string _connectionString;

        public SqliteConnection Connection { get; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Node>()
                .HasMany(node => node.ChildrenNodes)
                .WithOne(node => node.Parent)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Node>()
                .HasMany(node => node.ChildrenLeafs)
                .WithOne(leaf => leaf.Parent)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Node>()
                .HasMany(node => node.Parameters)
                .WithOne()
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Leaf>()
                .HasMany(leaf => leaf.Parameters)
                .WithOne()
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Data>()
               .HasOne<Leaf>()
               .WithOne()
               .HasForeignKey<Leaf>(leaf => leaf.DataID)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
        }

        public Node? GetParent(Node node)
        {
            if (node.ID == 1)
            {
                return null;
            }
            return node.Parent;
        }

        public async Task UpdateLastWriteTimeAsync(Node node, DateTime lastWriteTime)
        {
            node.LastWriteTime = lastWriteTime;
            var parent = GetParent(node);
            if (parent != null)
            {
                await UpdateLastWriteTimeAsync(parent, lastWriteTime);
            }
        }

        public async Task UpdateLastWriteTimeAsync(Leaf leaf, DateTime lastWriteTime)
        {
            leaf.LastWriteTime = lastWriteTime;
            await UpdateLastWriteTimeAsync(leaf.Parent, lastWriteTime);
        }

        public override void Dispose()
        {
            base.Dispose();
            Connection.Close();
            Connection.Dispose();
        }
    }

    public sealed class FileConnection :  IFileConnection
    {
        public FileConnection(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception("不存在文件");
            }
            _context = new FileFormatContext(filePath);
            var root = _context.Nodes.Find(1L);
            if (root == null)
            {
                root = new Node() { Name = "Root", LastWriteTime = DateTime.Now };
                _context.Nodes.Add(root);
                _context.SaveChanges();
                root = _context.Nodes.Find(1L);
            }
            _rootConnection = new NodeConnection(_context, root);
        }

        private readonly FileFormatContext _context;

        private readonly NodeConnection _rootConnection;

        public async Task<INodeConnection> ConnectNodeAsync(string name)
        {
            return await _rootConnection.ConnectNodeAsync(name);
        }

        public async Task<IDataConnection> ConnectDataAsync(string name)
        {
            return await _rootConnection.ConnectDataAsync(name);
        }

        public async Task<string[]> GetNodeNamesAsync()
        {
            return await _rootConnection.GetNodeNamesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }

    public sealed class NodeConnection : INodeConnection
    {
        public NodeConnection(FileFormatContext context, Node node)
        {
            _context = context;
            _node = node;
        }

        private readonly FileFormatContext _context;

        private readonly Node _node;

        public async Task<string[]> GetNodeNamesAsync()
        {
            return await _context.Entry(_node)
                  .Collection(node => node.ChildrenNodes)
                  .Query()
                  .AsNoTracking()
                  .Select(node => node.Name)
                  .ToArrayAsync();
        }

        public async Task<INodeConnection> ConnectNodeAsync(string name)
        {
            var node = await _context.Entry(_node)
                .Collection(node => node.ChildrenNodes)
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(node => node.Name == name);
            if (node == null)
            {
                node = new Node() { Name = name, Parent = _node };
                await _context.Nodes.AddAsync(node);
                await _context.UpdateLastWriteTimeAsync(node, DateTime.Now);
                await _context.SaveChangesAsync();
            }
            return new NodeConnection(_context, node);
        }

        public async Task<IDataConnection> ConnectDataAsync(string name)
        {
            var leaf = await _context.Entry(_node)
                   .Collection(n => n.ChildrenLeafs)
                   .Query()
                   .AsTracking()
                   .FirstOrDefaultAsync(n => n.Name == name);
            if (leaf == null)
            {
                leaf = new Leaf() { Name = name, Parent = _node };
                await _context.Leafs.AddAsync(leaf);
                await _context.UpdateLastWriteTimeAsync(leaf, DateTime.Now);
                await _context.SaveChangesAsync();
            }
            return new DataConnection(_context, leaf);
        }
    }

    public sealed class DataConnection : IDataConnection
    {
        public DataConnection(FileFormatContext context, Leaf leaf)
        {
            _context = context;
            _leaf = leaf;
        }

        private readonly FileFormatContext _context;

        private readonly Leaf _leaf;

        public Stream OpenWrite(long length)
        {
            var dataID = _leaf.DataID;
            if (dataID == null)
            {
                using (var command = _context.Connection.CreateCommand())
                {
                    command.CommandText =
                    @"
                        INSERT INTO Datas(Value)
                        VALUES (zeroblob($length));
                        UPDATE Leafs
                        SET DataID = last_insert_rowid()
                        WHERE ID = $id;
                        SELECT last_insert_rowid();
                    ";
                    command.Parameters.AddWithValue("$length", length);
                    command.Parameters.AddWithValue("$id", _leaf.ID);
                    dataID = (long)command.ExecuteScalar();
                    _leaf.DataID = dataID;
                }
            }
            else
            {
                using (var command = _context.Connection.CreateCommand())
                {
                    command.CommandText =
                    @"
                        SELECT length(Value)
                        FROM Datas
                        WHERE ID = $id
                    ";
                    command.Parameters.AddWithValue("$id", dataID);
                    var oldLength = (long)command.ExecuteScalar();
                    if(oldLength != length)
                    {
                        command.CommandText =
                        @"
                            UPDATE Datas
                            SET Value = zeroblob($length)
                            WHERE ID = $id;
                        ";
                        command.Parameters.AddWithValue("$length", length);
                        command.ExecuteNonQuery();
                    }
                }
            }
            return new SqliteBlob(_context.Connection, "Datas", "Value", dataID.Value);
        }

        public Stream OpenRead()
        {
            var dataID = _leaf.DataID;
            if (dataID == null)
            {
                throw new Exception("没有数据");
            }
            return new SqliteBlob(_context.Connection, "Datas", "Value", dataID.Value, true);
            //using (var command = _context.Connection.CreateCommand())
            //{
            //    command.CommandText =
            //    @"
            //        SELECT Value
            //        FROM Datas
            //        LIMIT $id
            //    ";
            //    command.Parameters.AddWithValue("$id", dataID);
            //    using (var reader = command.ExecuteReader())
            //    {
            //        reader.Read();
            //        return reader.GetStream(0);
            //    }
            //}
        }

        public async Task<string> GetParameterAsync(string name)
        {
            var parameter = await _context.Entry(_leaf)
                .Collection(leaf => leaf.Parameters)
                .Query()
                .AsNoTracking()
                .FirstAsync(parameter => parameter.Name == name);
            return parameter.Value;
        }

        public async Task SetParameterAsync(string name, string value)
        {
           var parameter = await _context.Entry(_leaf)
                .Collection(leaf => leaf.Parameters)
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(parameter => parameter.Name == name);
            if (parameter == null)
            {
                parameter = new LeafParameter() { Name = name, Value = value };
                _leaf.Parameters.Add(parameter);
            }
            else if (parameter.Value == value)
            {
                return;
            }
            else
            {
                parameter.Value = value;
            }
            await _context.SaveChangesAsync();
        }
    }
}
