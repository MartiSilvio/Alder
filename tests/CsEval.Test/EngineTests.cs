using NUnit.Framework;

namespace CsEval.Test
{
    [TestFixture]
    public class EngineTests
    {
        [Test]
        public void Evaluate_SimpleExpression()
        {
            var engine = new CsEvalEngine();
            var result = engine.Evaluate("1 + 2");
            Assert.That(result, Is.EqualTo(3L));
        }

        [Test]
        public void Evaluate_WithVariable()
        {
            var engine = new CsEvalEngine();
            engine.SetVariable("x", 10L);

            var result = engine.Evaluate("x * 2");
            Assert.That(result, Is.EqualTo(20L));
        }

        [Test]
        public void Evaluate_WithMultipleVariables()
        {
            var engine = new CsEvalEngine();
            engine.SetVariables(new Dictionary<string, object?>
            {
                ["a"] = 5L,
                ["b"] = 3L
            });

            var result = engine.Evaluate("a + b");
            Assert.That(result, Is.EqualTo(8L));
        }

        [Test]
        public void Evaluate_FluentApi()
        {
            var result = new CsEvalEngine()
                .SetVariable("x", 10L)
                .SetVariable("y", 5L)
                .Evaluate("x - y");

            Assert.That(result, Is.EqualTo(5L));
        }

        [Test]
        public void Evaluate_Generic_ReturnsTypedResult()
        {
            var engine = new CsEvalEngine();
            var result = engine.Evaluate<long>("1 + 2");
            Assert.That(result, Is.EqualTo(3L));
        }

        [Test]
        public void Evaluate_Generic_ConvertsType()
        {
            var engine = new CsEvalEngine();
            var result = engine.Evaluate<double>("10");
            Assert.That(result, Is.EqualTo(10.0));
        }

        [Test]
        public void Evaluate_MathProxy_IsRegistered()
        {
            var engine = new CsEvalEngine();
            var result = engine.Evaluate("Math.Abs(-5)");
            Assert.That(result, Is.EqualTo(5.0));
        }

        [Test]
        public void Evaluate_DateTimeProxy_IsRegistered()
        {
            var engine = new CsEvalEngine();
            var result = engine.Evaluate("DateTime.Now");
            Assert.That(result, Is.InstanceOf<DateTime>());
        }

        [Test]
        public void Evaluate_GuidProxy_IsRegistered()
        {
            var engine = new CsEvalEngine();
            var result = engine.Evaluate("Guid.NewGuid()");
            Assert.That(result, Is.InstanceOf<Guid>());
        }

        [Test]
        public void Evaluate_CustomFunction()
        {
            var engine = new CsEvalEngine();
            engine.RegisterFunction("double", args => Convert.ToInt64(args[0]) * 2);

            var result = engine.Evaluate("double(5)");
            Assert.That(result, Is.EqualTo(10L));
        }

        [Test]
        public void Evaluate_CustomProxy()
        {
            var engine = new CsEvalEngine();
            engine.RegisterProxy("Custom", new CustomProxy());

            var result = engine.Evaluate("Custom.Greet(\"World\")");
            Assert.That(result, Is.EqualTo("Hello, World!"));
        }

        [Test]
        public void Evaluate_ComplexExpression()
        {
            var engine = new CsEvalEngine();
            engine.SetVariable("items", new List<object?> { 1L, 2L, 3L, 4L, 5L });

            var result = engine.Evaluate("items.Where((x) => x > 2).Select((x) => x * 2)") as List<object?>;
            Assert.That(result, Is.EqualTo(new List<object?> { 6L, 8L, 10L }));
        }

        [Test]
        public void Evaluate_InterpolatedString()
        {
            var engine = new CsEvalEngine();
            engine.SetVariable("name", "CsEval");

            var result = engine.Evaluate("$\"Hello, {name}!\"");
            Assert.That(result, Is.EqualTo("Hello, CsEval!"));
        }

        [Test]
        public void Evaluate_AnonymousObject()
        {
            var engine = new CsEvalEngine();
            var result = engine.Evaluate("new { Name = \"Test\", Value = 42 }") as IDictionary<string, object?>;

            Assert.That(result, Is.Not.Null);
            Assert.That(result!["Name"], Is.EqualTo("Test"));
            Assert.That(result["Value"], Is.EqualTo(42L));
        }

        [Test]
        public void Evaluate_Block()
        {
            var engine = new CsEvalEngine();
            var result = engine.Evaluate("{ var x = 10; var y = 20; return x + y; }");
            Assert.That(result, Is.EqualTo(30L));
        }

        private class CustomProxy
        {
            public string Greet(string name) => $"Hello, {name}!";
        }
    }
}
