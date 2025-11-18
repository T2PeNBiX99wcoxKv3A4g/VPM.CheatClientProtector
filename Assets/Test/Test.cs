using System.Diagnostics.CodeAnalysis;
using io.github.ykysnk.CheatClientProtector;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon.Common.Interfaces;

namespace Test
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Test : CheatClientProtectorBehaviour
    {
        protected override float InteractDistance => 3;

        public override void Interact() => InteractCheck();

        protected override void InteractAntiCheat()
        {
            AnyOneCanCall("Interact Test");

            OnlyCanBeCallFromInteract("Interact Test", RandomKey);

            RequestCallMethod(nameof(InteractAntiCheat), nameof(OnlyCanBeCallFromInteractWithDelayed));
            SendCustomEventDelayedSeconds(nameof(OnlyCanBeCallFromInteractWithDelayed), 2);

            RequestCallMethodToAll(nameof(InteractAntiCheat), nameof(OnlyCanBeCallFromInteractWithNetwork));
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnlyCanBeCallFromInteractWithNetwork));

            RequestCallMethodToAll(nameof(InteractAntiCheat), nameof(OnlyCanBeCallFromInteractWithNetwork2));
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnlyCanBeCallFromInteractWithNetwork2),
                "Interact Test");
        }

        // Always can be called from run custom event. you can just call it from inspector.
        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public void AnyOneCanCall(string someTestArg)
        {
            Debug.Log($"AnyOneCanCall {someTestArg}");
        }

        // Only can be called from interacting.
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public void OnlyCanBeCallFromInteract(string someTestArg, int key)
        {
            if (!IsKeyCorrect(key)) return;
            Debug.Log($"OnlyCanBeCallFromInteract {someTestArg} {key}");
        }

        public void OnlyCanBeCallFromInteractWithDelayed()
        {
            if (!IsMethodHaveRequest(nameof(InteractAntiCheat), nameof(OnlyCanBeCallFromInteractWithDelayed))) return;
            Debug.Log("OnlyCanBeCallFromInteractWithDelayed");
        }

        public void OnlyCanBeCallFromInteractWithNetwork()
        {
            if (!IsMethodHaveRequest(nameof(InteractAntiCheat), nameof(OnlyCanBeCallFromInteractWithNetwork))) return;
            Debug.Log("OnlyCanBeCallFromInteractWithNetwork");
        }

        [NetworkCallable]
        public void OnlyCanBeCallFromInteractWithNetwork2(string someTestArg)
        {
            if (!IsMethodHaveRequest(nameof(InteractAntiCheat), nameof(OnlyCanBeCallFromInteractWithNetwork2))) return;
            Debug.Log($"OnlyCanBeCallFromInteractWithNetwork {someTestArg}");
        }

        public void OnlyCanBeCallFromOther(string someTestArg, int key)
        {
            if (!IsKeyCorrect(key)) return;
            Debug.Log($"OnlyCanBeCallFromOther {someTestArg}");
        }
    }
}