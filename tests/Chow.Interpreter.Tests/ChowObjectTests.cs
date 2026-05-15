using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Values;

namespace Chow.Interpreter.Tests
{
    [TestFixture]
    public class ChowObjectTests
    {
        [Test]
        public void Source_ReadsHostAssignedAttribute()
        {
            var module = new ChowModule();
            var obj = new ChowObject("gameobject");
            obj["health"] = 10;
            module["game_object"] = obj;

            module.Execute("__result = game_object.health");

            Assert.That(module.GetGlobal("__result").AsType<long>(), Is.EqualTo(10));
        }

        [Test]
        public void Source_AssignsExistingAndNewAttributes_HostCanReadBack()
        {
            var module = new ChowModule();
            var obj = new ChowObject("gameobject");
            obj["health"] = 10;
            module["game_object"] = obj;

            module.Execute("game_object.health = 25\ngame_object.name = 'player'");

            Assert.Multiple(() =>
            {
                Assert.That(obj.GetAttribute("health").AsType<long>(), Is.EqualTo(25));
                Assert.That(((ChowStr)obj.GetAttribute("name")).Value, Is.EqualTo("player"));
                Assert.That(obj.ContainsAttribute("name"), Is.True);
            });
        }

        [Test]
        public void Source_ReadsNestedChowObjectAttributes()
        {
            var module = new ChowModule();
            var obj = new ChowObject("gameobject");
            var transform = new ChowObject("transform");
            var position = new ChowObject("position");
            position["x"] = 3;
            transform["position"] = position;
            obj["transform"] = transform;
            module["game_object"] = obj;

            module.Execute("__result = game_object.transform.position.x");

            Assert.That(module.GetGlobal("__result").AsType<long>(), Is.EqualTo(3));
        }

        [Test]
        public void Source_AssignsNestedChowObjectAttribute_HostCanReadBack()
        {
            var module = new ChowModule();
            var obj = new ChowObject("gameobject");
            var transform = new ChowObject("transform");
            var position = new ChowObject("position");
            transform["position"] = position;
            obj["transform"] = transform;
            module["game_object"] = obj;

            module.Execute("game_object.transform.position.y = 4");

            Assert.That(position.GetAttribute("y").AsType<long>(), Is.EqualTo(4));
        }

        [Test]
        public void RoundTrip_ModuleGlobalAndAttributeRead_ReturnSameChowObjectWrappers()
        {
            var module = new ChowModule();
            var obj = new ChowObject("gameobject");
            var transform = new ChowObject("transform");
            obj["transform"] = transform;
            module["game_object"] = obj;

            module.Execute("__result = game_object.transform");

            Assert.Multiple(() =>
            {
                Assert.That(module.GetGlobal("game_object"), Is.SameAs(obj));
                Assert.That(module["game_object"], Is.SameAs(obj));
                Assert.That(module.GetGlobal("__result"), Is.SameAs(transform));
                Assert.That(obj.GetAttribute("transform"), Is.SameAs(transform));
                Assert.That(obj["transform"], Is.SameAs(transform));
            });
        }

        [Test]
        public void Source_ReadsUnknownAttribute_ThrowsAttributeException()
        {
            var module = new ChowModule();
            module["game_object"] = new ChowObject("gameobject");

            Assert.That(() => module.Execute("game_object.missing"), Throws.TypeOf<AttributeException>());
        }
    }
}
