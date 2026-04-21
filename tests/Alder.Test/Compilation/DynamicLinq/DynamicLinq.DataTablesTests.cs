using System.Data;
using Alder.Diagnostics;

namespace Alder.Test.Compilation;

public partial class DynamicLinqTests
{
    [TestFixture]
    [NonParallelizable]
    public class DataTables : CompilerFixtureBase
    {
        [Test]
        public void IEnumerable_DataRow_WhereDynamic_StringIndexerFilter_Composes()
        {
            var rows = CreateOrdersTable().AsEnumerable();

            var result = rows
                .WhereDynamic<DataRow>("(string)it[\"City\"] == \"Seattle\"")
                .Select(row => row.Field<string>("City"))
                .ToList();

            Assert.That(result, Is.EqualTo(new[] { "Seattle", "Seattle" }));
        }

        [Test]
        public void IEnumerable_DataRow_WhereDynamic_IntIndexerFilter_Composes()
        {
            var rows = CreateOrdersTable().AsEnumerable();

            var result = rows
                .WhereDynamic<DataRow>("(int)it[\"Size\"] == 3")
                .Select(row => row.Field<int>("Size"))
                .ToList();

            Assert.That(result, Is.EqualTo(new[] { 3, 3 }));
        }

        [Test]
        public void IQueryable_DataRow_WhereDynamic_IntIndexerFilter_Composes()
        {
            var rows = CreateOrdersTable().AsEnumerable().AsQueryable();

            var result = rows
                .WhereDynamic("row => (int)row[\"Size\"] == 3")
                .Select(row => row.Field<int>("Size"))
                .ToList();

            Assert.That(result, Is.EqualTo(new[] { 3, 3 }));
        }

        [Test]
        public void IQueryable_DataRow_OrderByDynamic_StringIndexer_Composes()
        {
            var rows = CreateOrdersTable().AsEnumerable().AsQueryable();

            var result = rows
                .OrderByDynamic<DataRow, string>("(string)it[\"City\"]")
                .Select(row => row.Field<string>("City"))
                .ToList();

            Assert.That(result, Is.EqualTo(new[] { "Austin", "Seattle", "Seattle", "Zurich" }));
        }

        [Test]
        public void IQueryable_DataRow_SelectDynamic_ObjectProjection_Composes()
        {
            var rows = CreateOrdersTable().AsEnumerable().AsQueryable();

            var result = rows
                .SelectDynamic<DataRow, object>("""new { City = (string)it["City"], Size = (int)it["Size"] }""")
                .Cast<object>()
                .ToList();

            Assert.That(result, Has.Count.EqualTo(4));
            var first = (IReadOnlyDictionary<string, object?>)result[0];
            var last = (IReadOnlyDictionary<string, object?>)result[3];
            Assert.That(first["City"], Is.EqualTo("Seattle"));
            Assert.That(first["Size"], Is.EqualTo(3));
            Assert.That(last["City"], Is.EqualTo("Zurich"));
            Assert.That(last["Size"], Is.EqualTo(3));
        }

        [Test]
        public void IQueryable_DataRow_WhereDynamic_FieldExtension_IsBlockedByDefaultSandbox()
        {
            using var engine = new AlderEngine(options =>
            {
                options.UseCompiler();
                options.Types.AddAssembly(typeof(DataRowExtensions).Assembly);
                options.Types.AddExtensionMethods(typeof(DataRowExtensions));
            });

            var rows = CreateOrdersTable().AsEnumerable().AsQueryable();

            var ex = Assert.Throws<AlderException>(() =>
                rows.WhereDynamic(engine, "row => row.Field<int>(\"Size\") == 3").ToList());

            Assert.That(ex, Is.Not.Null);
            Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
        }

        [Test]
        public void IQueryable_DataRow_WhereDynamic_FieldExtension_IntColumn_Composes_WhenSystemDataTrusted()
        {
            using var engine = new AlderEngine(options =>
            {
                options.UseCompiler();
                options.Types.AddAssembly(typeof(DataRowExtensions).Assembly);
                options.Types.AddExtensionMethods(typeof(DataRowExtensions));
                options.Sandbox = SandboxOptions.Safe() with
                {
                    TrustedNamespaces = ["System.Data"]
                };
            });

            var rows = CreateOrdersTable().AsEnumerable().AsQueryable();

            var result = rows
                .WhereDynamic(engine, "row => row.Field<int>(\"Size\") == 3")
                .Select(row => row.Field<int>("Size"))
                .ToList();

            Assert.That(result, Is.EqualTo(new[] { 3, 3 }));
        }

        private static DataTable CreateOrdersTable()
        {
            var table = new DataTable();
            table.Columns.Add("City", typeof(string));
            table.Columns.Add("Size", typeof(int));

            table.Rows.Add("Seattle", 3);
            table.Rows.Add("Austin", 1);
            table.Rows.Add("Seattle", 7);
            table.Rows.Add("Zurich", 3);

            return table;
        }
    }
}
