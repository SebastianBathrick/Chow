using Chow.Interpreter;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Hooks;
using Chow.Interpreter.State.Values;
using Chow.Interpreter.Values;
using System;
using System.Collections.Generic;

namespace Chow.Interpreter.ImplementationTests
{
    [TestFixture]
    public class InteropClassObjectExecutionTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        sealed class CaptureExprHook : IExpressionStatementHook
        {
            public List<ChowValue> Values { get; } = new List<ChowValue>();

            public void Invoke(object value = null)
            {
                Values.Add((ChowValue)value);
            }
        }

        sealed class TestGameObject : InteropClassObject
        {
            bool _activeSelf;
            readonly string _name;

            public TestGameObject(string name, bool activeSelf)
            {
                _name = name;
                _activeSelf = activeSelf;
            }

            public bool ActiveSelf
            {
                get { return _activeSelf; }
                set { _activeSelf = value; }
            }

            public override string ClassName => "GameObject";

            protected override IEnumerable<(string name, Func<TaggedUnion[], TaggedUnion> fn)> GetInitMethods()
            {
                yield return ("set_active", SetActive);
            }

            protected override IEnumerable<(string name, Field field)> GetInitFields()
            {
                yield return ("active_self", new Field(
                    () => new TaggedUnion(_activeSelf),
                    v => _activeSelf = v.BooleanValue));
                yield return ("name", new Field(
                    () => new TaggedUnion(_name),
                    null));
            }

            TaggedUnion SetActive(TaggedUnion[] args)
            {
                _activeSelf = args[0].BooleanValue;
                return TaggedUnion.None;
            }
        }

        static (ChowModule module, CaptureExprHook hook, TestGameObject obj) NewModule(string name = "thing", bool active = false)
        {
            var module = new ChowModule();
            var hook = new CaptureExprHook();
            module.AddHook(hook);
            var obj = new TestGameObject(name, active);
            module["game_object"] = obj;
            return (module, hook, obj);
        }

        static ChowValue Last(CaptureExprHook hook) => hook.Values[hook.Values.Count - 1];

        // ============================================================================================================
        // A. Field reads
        // ============================================================================================================

        [Test]
        public void ReadField_Writable_ReturnsInitialValue()
        {
            (var module, var hook, _) = NewModule(active: true);
            module.Execute("game_object.active_self");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        [Test]
        public void ReadField_ReadOnly_ReturnsValue()
        {
            (var module, var hook, _) = NewModule(name: "player");
            module.Execute("game_object.name");
            Assert.That(((ChowStr)Last(hook)).Value, Is.EqualTo("player"));
        }

        [Test]
        public void ReadField_LiveSemantics_ReflectsExternalMutation()
        {
            (var module, var hook, var obj) = NewModule(active: false);
            module.Execute("game_object.active_self");
            Assert.That(Last(hook).As<bool>(), Is.False);

            obj.ActiveSelf = true;
            module.Execute("game_object.active_self");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        // ============================================================================================================
        // B. Field writes
        // ============================================================================================================

        [Test]
        public void WriteField_Writable_ReflectsInSubsequentRead()
        {
            (var module, var hook, _) = NewModule(active: false);
            module.Execute("game_object.active_self = True\ngame_object.active_self");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        [Test]
        public void WriteField_Writable_ReflectsInUnderlyingCSharpState()
        {
            (var module, _, var obj) = NewModule(active: false);
            module.Execute("game_object.active_self = True");
            Assert.That(obj.ActiveSelf, Is.True);
        }

        [Test]
        public void WriteField_ReadOnly_ThrowsAttributeError()
        {
            (var module, _, _) = NewModule();
            var ex = Assert.Throws<AttributeException>(() => module.Execute("game_object.name = 'x'"));
            Assert.That(ex.Message, Does.Contain("read-only"));
        }

        // ============================================================================================================
        // C. Methods
        // ============================================================================================================

        [Test]
        public void MethodCall_MutatesUnderlyingState()
        {
            (var module, var hook, var obj) = NewModule(active: false);
            module.Execute("game_object.set_active(True)\ngame_object.active_self");
            Assert.That(Last(hook).As<bool>(), Is.True);
            Assert.That(obj.ActiveSelf, Is.True);
        }

        [Test]
        public void BoundMethod_StoredInVariable_StillBoundToOriginalInstance()
        {
            (var module, var hook, _) = NewModule(active: true);
            module.Execute("f = game_object.set_active\nf(False)\ngame_object.active_self");
            Assert.That(Last(hook).As<bool>(), Is.False);
        }

        [Test]
        public void WriteMethodName_ThrowsAttributeError()
        {
            (var module, _, _) = NewModule();
            var ex = Assert.Throws<AttributeException>(() => module.Execute("game_object.set_active = 1"));
            Assert.That(ex.Message, Does.Contain("read-only"));
        }

        // ============================================================================================================
        // D. Unknown attributes
        // ============================================================================================================

        [Test]
        public void ReadUnknownAttr_ThrowsAttributeError()
        {
            (var module, _, _) = NewModule();
            Assert.That(() => module.Execute("game_object.fake"), Throws.TypeOf<AttributeException>());
        }

        [Test]
        public void WriteUnknownAttr_ThrowsAttributeError()
        {
            (var module, _, _) = NewModule();
            Assert.That(() => module.Execute("game_object.fake = 1"), Throws.TypeOf<AttributeException>());
        }

        // ============================================================================================================
        // E. Truthiness (Python parity for Tag.Object)
        // ============================================================================================================

        [Test]
        public void Truthiness_InstanceIsTruthy()
        {
            // Python parity: an object instance is truthy. `not game_object` should be False.
            (var module, var hook, _) = NewModule();
            module.Execute("not game_object");
            Assert.That(Last(hook).As<bool>(), Is.False);
        }
    }
}
