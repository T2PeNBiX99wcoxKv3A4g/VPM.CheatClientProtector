using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using io.github.ykysnk.utils.Extensions;
using JetBrains.Annotations;
using UdonSharp;
using VRC.SDK3.Data;
using VRC.SDKBase;
using Random = System.Random;

namespace io.github.ykysnk.CheatClientProtector
{
    [PublicAPI]
    public abstract class CheatClientProtectorBehaviour : UdonSharpBehaviour
    {
        private readonly DataDictionary _keyCheckPool = new DataDictionary();
        private int _keyCheck;
        [UdonSynced] private int _keyCheckSync;
        private int _randomKey;
        protected virtual float InteractDistance => 3;

        protected int RandomKey
        {
            get
            {
                var random = new Random(DateTime.Now.Millisecond * _randomKey);
                return _randomKey = random.Next(int.MinValue, int.MaxValue);
            }
        }

        [SuppressMessage("ReSharper", "UnusedVariable")]
        protected bool IsKeyCorrect(int key)
        {
            var correct = key == _randomKey;
            var newKey = RandomKey;
            return correct;
        }

        protected bool IsKeyCorrect()
        {
            var keyCheck = _keyCheck;
            _keyCheck = 0;
            return IsKeyCorrect(keyCheck);
        }

        protected bool IsKeyCorrectSync()
        {
            var keyCheck = _keyCheckSync;
            _keyCheckSync = 0;
            return IsKeyCorrect(keyCheck);
        }

        protected bool IsMethodHaveRequest(string senderMethodName, string receiverMethodName)
        {
            // Kinda useless, I'm lazy to convert it to sha256
            senderMethodName = Base64(senderMethodName);
            receiverMethodName = Base64(receiverMethodName);

            if (!_keyCheckPool.TryGetValue(receiverMethodName, TokenType.DataDictionary, out var listToken))
                return false;

            var dictionary = listToken.DataDictionary;

            if (!dictionary.TryGetValue(senderMethodName, out var valueToken))
                return false;

            dictionary[senderMethodName] = valueToken.Int - 1;

            if (dictionary[senderMethodName].Int < 1)
                dictionary.Remove(senderMethodName);
            return true;
        }

        // Anyone can fake the senderMethodName, but this method is not public, so maybe it's safe.
        protected void RequestCallMethod(string senderMethodName, string receiverMethodName)
        {
            // Kinda useless, I'm lazy to convert it to sha256
            senderMethodName = Base64(senderMethodName);
            receiverMethodName = Base64(receiverMethodName);

            if (!_keyCheckPool.TryGetValue(receiverMethodName, TokenType.DataDictionary, out var listToken))
            {
                _keyCheckPool.Add(receiverMethodName, new DataDictionary());
                listToken = _keyCheckPool[receiverMethodName];
            }

            var dictionary = listToken.DataDictionary;

            if (!dictionary.TryGetValue(senderMethodName, out var valueToken))
            {
                dictionary.Add(senderMethodName, 0);
                valueToken = dictionary[senderMethodName];
            }

            dictionary[senderMethodName] = valueToken.Int + 1;
        }

        protected void RequestKeyCheck() => _keyCheck = RandomKey;

        protected void RequestKeyCheckSync() => _keyCheckSync = RandomKey;

        protected string Base64(object value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value.ToString()));

        protected void InteractCheck()
        {
            if (!Utilities.IsValid(gameObject) || !gameObject.IsPlayerCloseRange(InteractDistance)) return;
            InteractAntiCheat();
        }

        protected void InteractCheck2D()
        {
            if (!Utilities.IsValid(gameObject) || !gameObject.IsPlayerCloseRange2D(InteractDistance)) return;
            InteractAntiCheat();
        }

        protected virtual void InteractAntiCheat()
        {
        }
    }
}