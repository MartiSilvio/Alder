using System.Linq.Expressions;
using Alder.Compiled.Compilation;
using Alder.Runtime;

namespace Alder.Test.Compilation.DynamicLinq;

public partial class DynamicLinqTests
{
    [TestFixture]
    [NonParallelizable]
    public class InferredLambda : CompilerFixtureBase
    {
        [Test]
        public void ParsePredicateExpression_InferredDescriptor_ReportsBoolScalar()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            var parameter = Expression.Parameter(typeof(Product), "it");

            var prepared = QueryExpressionPreparer.PrepareDynamicQueryLambda(
                engine,
                [parameter],
                "Price > 50m",
                values: null,
                enableImplicitReceiver: true,
                expectedKind: DynamicQueryLambdaKind.Predicate);

            Assert.That(prepared.Kind, Is.EqualTo(DynamicQueryLambdaKind.Predicate));
            Assert.That(prepared.ResultType, Is.EqualTo(typeof(bool)));
            Assert.That(prepared.ResultShape, Is.EqualTo(DynamicQueryResultShape.Scalar));
            Assert.That(prepared.ExportedLambda, Is.InstanceOf<Expression<Func<Product, bool>>>());
            Assert.That(prepared.ExportedLambda.Body.Type, Is.EqualTo(typeof(bool)));
        }

        [Test]
        public void ParseSelectorExpression_InferredDescriptor_ReportsScalarResultType()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            var parameter = Expression.Parameter(typeof(Product), "it");

            var prepared = QueryExpressionPreparer.PrepareDynamicQueryLambda(
                engine,
                [parameter],
                "Name",
                values: null,
                enableImplicitReceiver: true,
                expectedKind: DynamicQueryLambdaKind.Selector);

            Assert.That(prepared.Kind, Is.EqualTo(DynamicQueryLambdaKind.Selector));
            Assert.That(prepared.ResultType, Is.EqualTo(typeof(string)));
            Assert.That(prepared.ResultShape, Is.EqualTo(DynamicQueryResultShape.Scalar));
            Assert.That(prepared.ExportedLambda.ReturnType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void ParseSelectorExpression_InferredDescriptor_ReportsStructuralResultShape()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            var parameter = Expression.Parameter(typeof(Product), "it");

            var prepared = QueryExpressionPreparer.PrepareDynamicQueryLambda(
                engine,
                [parameter],
                "new { Name, Price }",
                values: null,
                enableImplicitReceiver: true,
                expectedKind: DynamicQueryLambdaKind.Selector);

            Assert.That(prepared.Kind, Is.EqualTo(DynamicQueryLambdaKind.Selector));
            Assert.That(prepared.ResultType, Is.EqualTo(typeof(StructuralObjectValue)));
            Assert.That(prepared.ResultShape, Is.EqualTo(DynamicQueryResultShape.StructuralObject));
            Assert.That(prepared.ExportedLambda.ReturnType, Is.EqualTo(typeof(StructuralObjectValue)));
        }

        [Test]
        public void ParseSelectorExpression_InferredDescriptor_ReportsCollectionResultShape()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            var parameter = Expression.Parameter(typeof(Customer), "it");

            var prepared = QueryExpressionPreparer.PrepareDynamicQueryLambda(
                engine,
                [parameter],
                "Orders",
                values: null,
                enableImplicitReceiver: true,
                expectedKind: DynamicQueryLambdaKind.CollectionSelector);

            Assert.That(prepared.Kind, Is.EqualTo(DynamicQueryLambdaKind.CollectionSelector));
            Assert.That(prepared.ResultType, Is.EqualTo(typeof(List<Order>)));
            Assert.That(prepared.ResultShape, Is.EqualTo(DynamicQueryResultShape.Collection));
            Assert.That(prepared.ExportedLambda.ReturnType, Is.EqualTo(typeof(List<Order>)));
        }

        [Test]
        public void ParseLambdaExpression_InferredDescriptor_ReportsBinarySelectorResultType()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            var outer = Expression.Parameter(typeof(Customer), "outer");
            var inner = Expression.Parameter(typeof(Order), "inner");

            var prepared = QueryExpressionPreparer.PrepareDynamicQueryLambda(
                engine,
                [outer, inner],
                """outer.Name + ":" + inner.Product""",
                values: null,
                enableImplicitReceiver: false,
                expectedKind: DynamicQueryLambdaKind.BinarySelector);

            Assert.That(prepared.Kind, Is.EqualTo(DynamicQueryLambdaKind.BinarySelector));
            Assert.That(prepared.ResultType, Is.EqualTo(typeof(string)));
            Assert.That(prepared.ResultShape, Is.EqualTo(DynamicQueryResultShape.Scalar));
            Assert.That(prepared.ExportedLambda.ReturnType, Is.EqualTo(typeof(string)));
        }
    }
}
