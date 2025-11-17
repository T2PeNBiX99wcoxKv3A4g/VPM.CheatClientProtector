using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using io.github.ykysnk.utils.Extensions;
using JetBrains.Annotations;
using UdonSharp;
using VRC.SDK3.Data;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using Random = System.Random;

namespace io.github.ykysnk.CheatClientProtector
{
    [PublicAPI]
    public abstract class CheatClientProtectorBehaviour : UdonSharpBehaviour
    {
        private readonly DataDictionary _keyCheckPool = new DataDictionary();
        private int _keyCheck;
        private int _keyCheckPublic;
        private int _randomKey;
        private int _randomKeyPublic;
        protected virtual float InteractDistance => 3;

        /// <summary>
        ///     Generates a pseudo-random key based on the current time and the internal random key value.
        /// </summary>
        protected int RandomKey
        {
            get
            {
                var random = new Random(DateTime.Now.Millisecond * _randomKey);
                return _randomKey = random.Next(int.MinValue, int.MaxValue);
            }
        }

        /// <summary>
        ///     Generates a public pseudo-random key by combining the current time-based seed with an internal key value.
        /// </summary>
        public int RandomKeyPublic
        {
            get
            {
                var random = new Random(DateTime.Now.Millisecond * _randomKeyPublic);
                return _randomKeyPublic = random.Next(int.MinValue, int.MaxValue);
            }
        }

        /// <summary>
        ///     Checks if the provided key is correct.
        /// </summary>
        /// <param name="key">The key to be validated.</param>
        /// <returns>True if the provided key matches the expected key; otherwise, false.</returns>
        [SuppressMessage("ReSharper", "UnusedVariable")]
        protected bool IsKeyCorrect(int key)
        {
            var correct = key == _randomKey;
            var newKey = RandomKey;
            return correct;
        }

        /// <summary>
        ///     Checks if the synchronized key is correct.
        /// </summary>
        /// <returns>True if the key is correct; otherwise, false.</returns>
        protected bool IsKeyCorrect()
        {
            var keyCheck = _keyCheck;
            _keyCheck = 0;
            return IsKeyCorrect(keyCheck);
        }

        /// <summary>
        ///     Check if the public key is correct.
        /// </summary>
        /// <param name="key">The public key to validate.</param>
        /// <returns>true if the public key is correct; otherwise, false.</returns>
        [SuppressMessage("ReSharper", "UnusedVariable")]
        protected bool IsPublicKeyCorrect(int key)
        {
            var correct = key == _randomKeyPublic;
            var newKey = RandomKeyPublic;
            return correct;
        }

        /// <summary>
        ///     Checks if the public key is correct.
        /// </summary>
        /// <returns>True if the synchronized public key is correct; otherwise, false.</returns>
        protected bool IsPublicKeyCorrect()
        {
            var keyCheck = _keyCheckPublic;
            _keyCheckPublic = 0;
            return IsPublicKeyCorrect(keyCheck);
        }

        /// <summary>
        ///     Verifies if a request exists between the specified sender and receiver methods.
        /// </summary>
        /// <param name="senderMethodName">The name of the sender method.</param>
        /// <param name="receiverMethodName">The name of the receiver method.</param>
        /// <returns>true if a request exists between the sender and receiver methods; otherwise, false.</returns>
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

        // Anyone can fake the senderMethodName, maybe is safe.
        /// <summary>
        ///     Tracks and increments the call count for a specific sender and receiver method pair.
        /// </summary>
        /// <param name="senderMethodName">The name of the method that initiated the request.</param>
        /// <param name="receiverMethodName">The name of the method intended to receive the request.</param>
        [NetworkCallable]
        public void RequestCallMethod([CanBeNull] string senderMethodName, [CanBeNull] string receiverMethodName)
        {
            if (string.IsNullOrEmpty(senderMethodName) || string.IsNullOrEmpty(receiverMethodName)) return;
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

        /// <summary>
        ///     Requests the invocation of the specified method on all clients in the network.
        /// </summary>
        /// <param name="senderMethodName">The name of the method initiating the request.</param>
        /// <param name="receiverMethodName">The name of the method to be invoked on all clients.</param>
        public void RequestCallMethodToAll(string senderMethodName, string receiverMethodName) =>
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RequestCallMethod), senderMethodName,
                receiverMethodName);

        /// <summary>
        ///     Stores a new randomized key for validation purposes.
        /// </summary>
        protected void RequestKeyCheck() => _keyCheck = RandomKey;

        /// <summary>
        ///     Updates the public key check value with a newly generated random public key.
        /// </summary>
        public void RequestKeyCheckPublic() => _keyCheckPublic = RandomKeyPublic;

        /// <summary>
        ///     Encodes the provided value into a Base64 string.
        /// </summary>
        /// <param name="value">
        ///     The object to be encoded into a Base64 string. The object's string representation will be used for
        ///     encoding.
        /// </param>
        /// <returns>The Base64 encoded string representation of the provided value.</returns>
        [CanBeNull]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        protected string Base64([CanBeNull] object value) => !Utilities.IsValid(value)
            ? null
            : Convert.ToBase64String(Encoding.UTF8.GetBytes(value.ToString()));

        /// <summary>
        ///     Verifies if a player is within the interactable range of the GameObject and performs anti-cheat interaction logic.
        /// </summary>
        protected void InteractCheck()
        {
            if (!Utilities.IsValid(gameObject) || !gameObject.IsPlayerCloseRange(InteractDistance)) return;
            InteractAntiCheat();
        }

        /// <summary>
        ///     Verifies whether the game object is within the 2D interaction range of the player,
        ///     and performs anti-cheat interactions if the validation passes.
        /// </summary>
        protected void InteractCheck2D()
        {
            if (!Utilities.IsValid(gameObject) || !gameObject.IsPlayerCloseRange2D(InteractDistance)) return;
            InteractAntiCheat();
        }

        /// <summary>
        ///     Performs anti-cheat logic during player interaction with the object.
        /// </summary>
        /// <remarks>
        ///     This method is called during player interaction, ensuring proper validation and protection
        ///     against unauthorized interactions. The specific anti-cheat behavior is defined in derived classes.
        /// </remarks>
        protected virtual void InteractAntiCheat()
        {
        }
    }
}