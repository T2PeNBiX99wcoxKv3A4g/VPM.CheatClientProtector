using io.github.ykysnk.CheatClientProtector;
using UdonSharp;

namespace Test
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Test2 : CheatClientProtectorBehaviour
    {
        public Test test;

        public override void Interact() => InteractCheck();

        protected override void InteractAntiCheat()
        {
            test.OnlyCanBeCallFromOther($"Call from {this}", test.RandomKey);
        }
    }
}