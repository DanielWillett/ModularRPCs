using System;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Protocol;
using UnityEngine;

namespace ModularRPCs.Test.SourceGen
{
    [GenerateRpcSource]
    public partial class UnityComponent : MonoBehaviour, IRpcObject<int>
    {
        /// <inheritdoc />
        public int Identifier { get; private set; }

        private void Awake()
        {
            Identifier = 3;
        }

        [RpcSend(nameof(ReceiveSomething))]
        public partial RpcTask SendSomething();

        [RpcReceive]
        private void ReceiveSomething(IModularRpcRemoteConnection connection)
        {

        }
    }
}
