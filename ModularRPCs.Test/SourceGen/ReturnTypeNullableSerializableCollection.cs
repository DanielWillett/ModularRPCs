using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModularRPCs.Test.CodeGen;

namespace ModularRPCs.Test.SourceGen
{
    [NonParallelizable, TestFixture]
    public partial class ReturnTypeNullableSerializableCollection
    {
        private static bool _wasInvoked;

        private const int RtnValueSerializableTypeStructFixedArrayLength = 6;
        private static readonly SerializableTypeStructFixed?[][] RtnValueSerializableTypeStructFixedArray =
            new[]
            {
                new SerializableTypeStructFixed?[]
                {
                    new SerializableTypeStructFixed
                    {
                        Value = 1
                    },
                    new SerializableTypeStructFixed
                    {
                        Value = 2
                    }
                },
                null,
                new SerializableTypeStructFixed?[0],
                new SerializableTypeStructFixed?[]
                {
                    new SerializableTypeStructFixed
                    {
                        Value = 1
                    }
                },
                new SerializableTypeStructFixed?[]
                {
                    null
                },
                new SerializableTypeStructFixed?[]
                {
                    new SerializableTypeStructFixed
                    {
                        Value = 1
                    },
                    null,
                    new SerializableTypeStructFixed
                    {
                        Value = 999
                    }
                },
            };

        private const int RtnValueSerializableTypeStructFixedArraySegmentLength = 10;
        private static readonly ArraySegment<SerializableTypeStructFixed?>[] RtnValueSerializableTypeStructFixedArraySegment =
            new[]
            {
                new ArraySegment<SerializableTypeStructFixed?>(new SerializableTypeStructFixed?[]
                {
                    new SerializableTypeStructFixed
                    {
                        Value = 1
                    },
                    new SerializableTypeStructFixed
                    {
                        Value = 2
                    }
                }),
                default,
                new ArraySegment<SerializableTypeStructFixed?>(new SerializableTypeStructFixed?[0]),
                new ArraySegment<SerializableTypeStructFixed?>(new SerializableTypeStructFixed?[]
                {
                    new SerializableTypeStructFixed
                    {
                        Value = 1
                    }
                }),
                new ArraySegment<SerializableTypeStructFixed?>(new SerializableTypeStructFixed?[]
                {
                    null
                }),
                new ArraySegment<SerializableTypeStructFixed?>(new SerializableTypeStructFixed?[]
                {
                    new SerializableTypeStructFixed
                    {
                        Value = 1
                    },
                    null,
                    new SerializableTypeStructFixed
                    {
                        Value = 999
                    }
                }),
                new ArraySegment<SerializableTypeStructFixed?>(new SerializableTypeStructFixed?[]
                {
                    new SerializableTypeStructFixed
                    {
                        Value = 1
                    },
                    new SerializableTypeStructFixed
                    {
                        Value = 2
                    },
                    new SerializableTypeStructFixed
                    {
                        Value = 3
                    },
                    new SerializableTypeStructFixed
                    {
                        Value = 4
                    }
                }, 1, 2),
                new ArraySegment<SerializableTypeStructFixed?>(new SerializableTypeStructFixed?[]
                {
                    new SerializableTypeStructFixed
                    {
                        Value = 1
                    }
                }, 0, 0),
                new ArraySegment<SerializableTypeStructFixed?>(new SerializableTypeStructFixed?[]
                {
                    null,
                    null,
                    null
                }, 1, 0),
                new ArraySegment<SerializableTypeStructFixed?>(new SerializableTypeStructFixed?[]
                {
                    new SerializableTypeStructFixed
                    {
                        Value = 1
                    },
                    null,
                    new SerializableTypeStructFixed
                    {
                        Value = 999
                    }
                }, 0, 2),
            };

        private const int RtnValueSerializableTypeStructFixedICollectionLength = RtnValueSerializableTypeStructFixedArrayLength;
        private static readonly CollectionImpl<SerializableTypeStructFixed?>[] RtnValueSerializableTypeStructFixedICollection =
            RtnValueSerializableTypeStructFixedArray.Select(x => x == null ? null : new CollectionImpl<SerializableTypeStructFixed?>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructFixedIReadOnlyCollectionLength = RtnValueSerializableTypeStructFixedArrayLength;
        private static readonly ReadOnlyCollectionImpl<SerializableTypeStructFixed?>[] RtnValueSerializableTypeStructFixedIReadOnlyCollection =
            RtnValueSerializableTypeStructFixedArray.Select(x => x == null ? null : new ReadOnlyCollectionImpl<SerializableTypeStructFixed?>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructFixedIEnumerableLength = RtnValueSerializableTypeStructFixedArrayLength;
        private static readonly EnumerableImpl<SerializableTypeStructFixed?>[] RtnValueSerializableTypeStructFixedIEnumerable =
            RtnValueSerializableTypeStructFixedArray.Select(x => x == null ? null : new EnumerableImpl<SerializableTypeStructFixed?>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructFixedListLength = RtnValueSerializableTypeStructFixedArrayLength;
        private static readonly List<SerializableTypeStructFixed?>[] RtnValueSerializableTypeStructFixedList =
            RtnValueSerializableTypeStructFixedArray.Select(x => x == null ? null : new List<SerializableTypeStructFixed?>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructVariableArrayLength = 6;
        private static readonly SerializableTypeStructVariable?[][] RtnValueSerializableTypeStructVariableArray =
            new[]
            {
                new SerializableTypeStructVariable?[]
                {
                    new SerializableTypeStructVariable
                    {
                        Value = "te1"
                    },
                    new SerializableTypeStructVariable
                    {
                        Value = "te2"
                    }
                },
                null,
                new SerializableTypeStructVariable?[0],
                new SerializableTypeStructVariable?[]
                {
                    new SerializableTypeStructVariable
                    {
                        Value = "test 1"
                    }
                },
                new SerializableTypeStructVariable?[]
                {
                    null
                },
                new SerializableTypeStructVariable?[]
                {
                    new SerializableTypeStructVariable
                    {
                        Value = "v 1"
                    },
                    null,
                    new SerializableTypeStructVariable
                    {
                        Value = "avakhfbkhsfkshf"
                    }
                },
            };

        private const int RtnValueSerializableTypeStructVariableArraySegmentLength = 10;
        private static readonly ArraySegment<SerializableTypeStructVariable?>[] RtnValueSerializableTypeStructVariableArraySegment =
            new[]
            {
                new ArraySegment<SerializableTypeStructVariable?>(new SerializableTypeStructVariable?[]
                {
                    new SerializableTypeStructVariable
                    {
                        Value = "afasfshf "
                    },
                    new SerializableTypeStructVariable
                    {
                        Value = " aifj "
                    }
                }),
                default,
                new ArraySegment<SerializableTypeStructVariable?>(new SerializableTypeStructVariable?[0]),
                new ArraySegment<SerializableTypeStructVariable?>(new SerializableTypeStructVariable?[]
                {
                    new SerializableTypeStructVariable
                    {
                        Value = " v 1 "
                    }
                }),
                new ArraySegment<SerializableTypeStructVariable?>(new SerializableTypeStructVariable?[]
                {
                    null
                }),
                new ArraySegment<SerializableTypeStructVariable?>(new SerializableTypeStructVariable?[]
                {
                    new SerializableTypeStructVariable
                    {
                        Value = ""
                    },
                    null,
                    new SerializableTypeStructVariable
                    {
                        Value = "V e8f"
                    }
                }),
                new ArraySegment<SerializableTypeStructVariable?>(new SerializableTypeStructVariable?[]
                {
                    new SerializableTypeStructVariable
                    {
                        Value = " "
                    },
                    new SerializableTypeStructVariable
                    {
                        Value = "lmao "
                    },
                    new SerializableTypeStructVariable
                    {
                        Value = "no "
                    },
                    new SerializableTypeStructVariable
                    {
                        Value = "v4"
                    }
                }, 1, 2),
                new ArraySegment<SerializableTypeStructVariable?>(new SerializableTypeStructVariable?[]
                {
                    new SerializableTypeStructVariable
                    {
                        Value = "text "
                    }
                }, 0, 0),
                new ArraySegment<SerializableTypeStructVariable?>(new SerializableTypeStructVariable?[]
                {
                    null,
                    null,
                    null
                }, 1, 0),
                new ArraySegment<SerializableTypeStructVariable?>(new SerializableTypeStructVariable?[]
                {
                    new SerializableTypeStructVariable
                    {
                        Value = " txt"
                    },
                    null,
                    new SerializableTypeStructVariable
                    {
                        Value = " t  e  x t "
                    }
                }, 0, 2),
            };

        private const int RtnValueSerializableTypeStructVariableICollectionLength = RtnValueSerializableTypeStructVariableArrayLength;
        private static readonly CollectionImpl<SerializableTypeStructVariable?>[] RtnValueSerializableTypeStructVariableICollection =
            RtnValueSerializableTypeStructVariableArray.Select(x => x == null ? null : new CollectionImpl<SerializableTypeStructVariable?>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructVariableIReadOnlyCollectionLength = RtnValueSerializableTypeStructVariableArrayLength;
        private static readonly ReadOnlyCollectionImpl<SerializableTypeStructVariable?>[] RtnValueSerializableTypeStructVariableIReadOnlyCollection =
            RtnValueSerializableTypeStructVariableArray.Select(x => x == null ? null : new ReadOnlyCollectionImpl<SerializableTypeStructVariable?>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructVariableIEnumerableLength = RtnValueSerializableTypeStructVariableArrayLength;
        private static readonly EnumerableImpl<SerializableTypeStructVariable?>[] RtnValueSerializableTypeStructVariableIEnumerable =
            RtnValueSerializableTypeStructVariableArray.Select(x => x == null ? null : new EnumerableImpl<SerializableTypeStructVariable?>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructVariableListLength = RtnValueSerializableTypeStructVariableArrayLength;
        private static readonly List<SerializableTypeStructVariable?>[] RtnValueSerializableTypeStructVariableList =
            RtnValueSerializableTypeStructVariableArray.Select(x => x == null ? null : new List<SerializableTypeStructVariable?>(x.ToList())).ToArray();

        [Test]
        public async Task ServerToClientBytesSerializableTypeStructArrayFixed([Range(0, RtnValueSerializableTypeStructFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructFixed?[] rtnValue = await proxy.InvokeFromServerSerializableClassArrayFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArray[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructArrayFixed([Range(0, RtnValueSerializableTypeStructFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructFixed?[] rtnValue = await proxy.InvokeFromClientSerializableClassArrayFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArray[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructArrayFixed([Range(0, RtnValueSerializableTypeStructFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructFixed?[] rtnValue = await proxy.InvokeFromServerSerializableClassArrayFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArray[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructArrayFixed([Range(0, RtnValueSerializableTypeStructFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructFixed?[] rtnValue = await proxy.InvokeFromClientSerializableClassArrayFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArray[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructArrayFixed([Range(0, RtnValueSerializableTypeStructFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructFixed?[] rtnValue = await proxy.InvokeTaskFromServerSerializableClassArrayFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArray[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructArrayFixed([Range(0, RtnValueSerializableTypeStructFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructFixed?[] rtnValue = await proxy.InvokeTaskFromClientSerializableClassArrayFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArray[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructArrayFixed([Range(0, RtnValueSerializableTypeStructFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructFixed?[] rtnValue = await proxy.InvokeTaskFromServerSerializableClassArrayFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArray[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructArrayFixed([Range(0, RtnValueSerializableTypeStructFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructFixed?[] rtnValue = await proxy.InvokeTaskFromClientSerializableClassArrayFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArray[version]));
        }



        [Test]
        public async Task ServerToClientBytesSerializableTypeStructArraySegmentFixed([Range(0, RtnValueSerializableTypeStructFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromServerSerializableClassArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructArraySegmentFixed([Range(0, RtnValueSerializableTypeStructFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromClientSerializableClassArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArraySegment[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructArraySegmentFixed([Range(0, RtnValueSerializableTypeStructFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromServerSerializableClassArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructArraySegmentFixed([Range(0, RtnValueSerializableTypeStructFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromClientSerializableClassArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArraySegment[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructArraySegmentFixed([Range(0, RtnValueSerializableTypeStructFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructArraySegmentFixed([Range(0, RtnValueSerializableTypeStructFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArraySegment[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructArraySegmentFixed([Range(0, RtnValueSerializableTypeStructFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructArraySegmentFixed([Range(0, RtnValueSerializableTypeStructFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArraySegment[version]));
        }



        [Test]
        public async Task ServerToClientBytesSerializableTypeStructICollectionFixed([Range(0, RtnValueSerializableTypeStructFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromServerSerializableClassICollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructICollectionFixed([Range(0, RtnValueSerializableTypeStructFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromClientSerializableClassICollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedICollection[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructICollectionFixed([Range(0, RtnValueSerializableTypeStructFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromServerSerializableClassICollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructICollectionFixed([Range(0, RtnValueSerializableTypeStructFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromClientSerializableClassICollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedICollection[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructICollectionFixed([Range(0, RtnValueSerializableTypeStructFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassICollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructICollectionFixed([Range(0, RtnValueSerializableTypeStructFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassICollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedICollection[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructICollectionFixed([Range(0, RtnValueSerializableTypeStructFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassICollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructICollectionFixed([Range(0, RtnValueSerializableTypeStructFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassICollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedICollection[version]));
        }




        [Test]
        public async Task ServerToClientBytesSerializableTypeStructIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeStructFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromServerSerializableClassIReadOnlyCollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeStructFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromClientSerializableClassIReadOnlyCollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeStructFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromServerSerializableClassIReadOnlyCollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeStructFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromClientSerializableClassIReadOnlyCollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeStructFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIReadOnlyCollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeStructFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIReadOnlyCollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeStructFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIReadOnlyCollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeStructFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIReadOnlyCollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIReadOnlyCollection[version]));
        }




        [Test]
        public async Task ServerToClientBytesSerializableTypeStructIEnumerableFixed([Range(0, RtnValueSerializableTypeStructFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromServerSerializableClassIEnumerableFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructIEnumerableFixed([Range(0, RtnValueSerializableTypeStructFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromClientSerializableClassIEnumerableFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIEnumerable[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructIEnumerableFixed([Range(0, RtnValueSerializableTypeStructFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromServerSerializableClassIEnumerableFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructIEnumerableFixed([Range(0, RtnValueSerializableTypeStructFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromClientSerializableClassIEnumerableFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIEnumerable[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructIEnumerableFixed([Range(0, RtnValueSerializableTypeStructFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIEnumerableFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructIEnumerableFixed([Range(0, RtnValueSerializableTypeStructFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIEnumerableFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIEnumerable[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructIEnumerableFixed([Range(0, RtnValueSerializableTypeStructFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIEnumerableFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructIEnumerableFixed([Range(0, RtnValueSerializableTypeStructFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIEnumerableFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedIEnumerable[version]));
        }




        [Test]
        public async Task ServerToClientBytesSerializableTypeStructListFixed([Range(0, RtnValueSerializableTypeStructFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromServerSerializableClassListFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedList[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructListFixed([Range(0, RtnValueSerializableTypeStructFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromClientSerializableClassListFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedList[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructListFixed([Range(0, RtnValueSerializableTypeStructFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromServerSerializableClassListFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedList[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructListFixed([Range(0, RtnValueSerializableTypeStructFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeFromClientSerializableClassListFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedList[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructListFixed([Range(0, RtnValueSerializableTypeStructFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassListFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedList[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructListFixed([Range(0, RtnValueSerializableTypeStructFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassListFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedList[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructListFixed([Range(0, RtnValueSerializableTypeStructFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassListFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedList[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructListFixed([Range(0, RtnValueSerializableTypeStructFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeStructFixed?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassListFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedList[version]));
        }





        [Test]
        public async Task ServerToClientBytesSerializableTypeStructArrayVariable([Range(0, RtnValueSerializableTypeStructVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructVariable?[] rtnValue = await proxy.InvokeFromServerSerializableClassArrayVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArray[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructArrayVariable([Range(0, RtnValueSerializableTypeStructVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructVariable?[] rtnValue = await proxy.InvokeFromClientSerializableClassArrayVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArray[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructArrayVariable([Range(0, RtnValueSerializableTypeStructVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructVariable?[] rtnValue = await proxy.InvokeFromServerSerializableClassArrayVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArray[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructArrayVariable([Range(0, RtnValueSerializableTypeStructVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructVariable?[] rtnValue = await proxy.InvokeFromClientSerializableClassArrayVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArray[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructArrayVariable([Range(0, RtnValueSerializableTypeStructVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructVariable?[] rtnValue = await proxy.InvokeTaskFromServerSerializableClassArrayVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArray[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructArrayVariable([Range(0, RtnValueSerializableTypeStructVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructVariable?[] rtnValue = await proxy.InvokeTaskFromClientSerializableClassArrayVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArray[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructArrayVariable([Range(0, RtnValueSerializableTypeStructVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructVariable?[] rtnValue = await proxy.InvokeTaskFromServerSerializableClassArrayVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArray[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructArrayVariable([Range(0, RtnValueSerializableTypeStructVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeStructVariable?[] rtnValue = await proxy.InvokeTaskFromClientSerializableClassArrayVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArray[version]));
        }



        [Test]
        public async Task ServerToClientBytesSerializableTypeStructArraySegmentVariable([Range(0, RtnValueSerializableTypeStructVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromServerSerializableClassArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructArraySegmentVariable([Range(0, RtnValueSerializableTypeStructVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromClientSerializableClassArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArraySegment[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructArraySegmentVariable([Range(0, RtnValueSerializableTypeStructVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromServerSerializableClassArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructArraySegmentVariable([Range(0, RtnValueSerializableTypeStructVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromClientSerializableClassArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArraySegment[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructArraySegmentVariable([Range(0, RtnValueSerializableTypeStructVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructArraySegmentVariable([Range(0, RtnValueSerializableTypeStructVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArraySegment[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructArraySegmentVariable([Range(0, RtnValueSerializableTypeStructVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructArraySegmentVariable([Range(0, RtnValueSerializableTypeStructVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArraySegment[version]));
        }



        [Test]
        public async Task ServerToClientBytesSerializableTypeStructICollectionVariable([Range(0, RtnValueSerializableTypeStructVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromServerSerializableClassICollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructICollectionVariable([Range(0, RtnValueSerializableTypeStructVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromClientSerializableClassICollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableICollection[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructICollectionVariable([Range(0, RtnValueSerializableTypeStructVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromServerSerializableClassICollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructICollectionVariable([Range(0, RtnValueSerializableTypeStructVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromClientSerializableClassICollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableICollection[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructICollectionVariable([Range(0, RtnValueSerializableTypeStructVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassICollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructICollectionVariable([Range(0, RtnValueSerializableTypeStructVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassICollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableICollection[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructICollectionVariable([Range(0, RtnValueSerializableTypeStructVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassICollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructICollectionVariable([Range(0, RtnValueSerializableTypeStructVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassICollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableICollection[version]));
        }




        [Test]
        public async Task ServerToClientBytesSerializableTypeStructIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeStructVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromServerSerializableClassIReadOnlyCollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeStructVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromClientSerializableClassIReadOnlyCollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeStructVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromServerSerializableClassIReadOnlyCollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeStructVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromClientSerializableClassIReadOnlyCollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeStructVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIReadOnlyCollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeStructVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIReadOnlyCollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeStructVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIReadOnlyCollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeStructVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIReadOnlyCollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIReadOnlyCollection[version]));
        }




        [Test]
        public async Task ServerToClientBytesSerializableTypeStructIEnumerableVariable([Range(0, RtnValueSerializableTypeStructVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromServerSerializableClassIEnumerableVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructIEnumerableVariable([Range(0, RtnValueSerializableTypeStructVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromClientSerializableClassIEnumerableVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIEnumerable[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructIEnumerableVariable([Range(0, RtnValueSerializableTypeStructVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromServerSerializableClassIEnumerableVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructIEnumerableVariable([Range(0, RtnValueSerializableTypeStructVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromClientSerializableClassIEnumerableVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIEnumerable[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructIEnumerableVariable([Range(0, RtnValueSerializableTypeStructVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIEnumerableVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructIEnumerableVariable([Range(0, RtnValueSerializableTypeStructVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIEnumerableVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIEnumerable[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructIEnumerableVariable([Range(0, RtnValueSerializableTypeStructVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIEnumerableVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructIEnumerableVariable([Range(0, RtnValueSerializableTypeStructVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIEnumerableVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableIEnumerable[version]));
        }




        [Test]
        public async Task ServerToClientBytesSerializableTypeStructListVariable([Range(0, RtnValueSerializableTypeStructVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromServerSerializableClassListVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableList[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructListVariable([Range(0, RtnValueSerializableTypeStructVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromClientSerializableClassListVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableList[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeStructListVariable([Range(0, RtnValueSerializableTypeStructVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromServerSerializableClassListVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableList[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructListVariable([Range(0, RtnValueSerializableTypeStructVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeFromClientSerializableClassListVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableList[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeStructListVariable([Range(0, RtnValueSerializableTypeStructVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassListVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableList[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructListVariable([Range(0, RtnValueSerializableTypeStructVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassListVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableList[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeStructListVariable([Range(0, RtnValueSerializableTypeStructVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromServerSerializableClassListVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableList[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructListVariable([Range(0, RtnValueSerializableTypeStructVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeStructVariable?> rtnValue = await proxy.InvokeTaskFromClientSerializableClassListVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableList[version]));
        }

        [RpcClass, GenerateRpcSource]
        public partial class TestClass
        {
            [RpcSend(nameof(ReceiveSerializableClassArrayFixed))]
            public partial RpcTask<SerializableTypeStructFixed?[]> InvokeFromClientSerializableClassArrayFixed(int version);

            [RpcSend(nameof(ReceiveSerializableClassArrayFixed))]
            public partial RpcTask<SerializableTypeStructFixed?[]> InvokeFromServerSerializableClassArrayFixed(int version, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableClassArrayFixedTask))]
            public partial RpcTask<SerializableTypeStructFixed?[]> InvokeTaskFromClientSerializableClassArrayFixed(int version);

            [RpcSend(nameof(ReceiveSerializableClassArrayFixedTask))]
            public partial RpcTask<SerializableTypeStructFixed?[]> InvokeTaskFromServerSerializableClassArrayFixed(int version, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private SerializableTypeStructFixed?[] ReceiveSerializableClassArrayFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructFixedArray[version];
            }

            [RpcReceive]
            private async Task<SerializableTypeStructFixed?[]> ReceiveSerializableClassArrayFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructFixedArray[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentFixed))]
            public partial RpcTask<ArraySegment<SerializableTypeStructFixed?>> InvokeFromClientSerializableClassArraySegmentFixed(int version);

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentFixed))]
            public partial RpcTask<ArraySegment<SerializableTypeStructFixed?>> InvokeFromServerSerializableClassArraySegmentFixed(int version, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentFixedTask))]
            public partial RpcTask<ArraySegment<SerializableTypeStructFixed?>> InvokeTaskFromClientSerializableClassArraySegmentFixed(int version);

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentFixedTask))]
            public partial RpcTask<ArraySegment<SerializableTypeStructFixed?>> InvokeTaskFromServerSerializableClassArraySegmentFixed(int version, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private ArraySegment<SerializableTypeStructFixed?> ReceiveSerializableClassArraySegmentFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructFixedArraySegment[version];
            }

            [RpcReceive]
            private async Task<ArraySegment<SerializableTypeStructFixed?>> ReceiveSerializableClassArraySegmentFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructFixedArraySegment[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassICollectionFixed))]
            public partial RpcTask<ICollection<SerializableTypeStructFixed?>> InvokeFromClientSerializableClassICollectionFixed(int version);

            [RpcSend(nameof(ReceiveSerializableClassICollectionFixed))]
            public partial RpcTask<ICollection<SerializableTypeStructFixed?>> InvokeFromServerSerializableClassICollectionFixed(int version, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableClassICollectionFixedTask))]
            public partial RpcTask<ICollection<SerializableTypeStructFixed?>> InvokeTaskFromClientSerializableClassICollectionFixed(int version);

            [RpcSend(nameof(ReceiveSerializableClassICollectionFixedTask))]
            public partial RpcTask<ICollection<SerializableTypeStructFixed?>> InvokeTaskFromServerSerializableClassICollectionFixed(int version, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private ICollection<SerializableTypeStructFixed?> ReceiveSerializableClassICollectionFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructFixedICollection[version];
            }

            [RpcReceive]
            private async Task<ICollection<SerializableTypeStructFixed?>> ReceiveSerializableClassICollectionFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructFixedICollection[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionFixed))]
            public partial RpcTask<IReadOnlyCollection<SerializableTypeStructFixed?>> InvokeFromClientSerializableClassIReadOnlyCollectionFixed(int version);

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionFixed))]
            public partial RpcTask<IReadOnlyCollection<SerializableTypeStructFixed?>> InvokeFromServerSerializableClassIReadOnlyCollectionFixed(int version, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionFixedTask))]
            public partial RpcTask<IReadOnlyCollection<SerializableTypeStructFixed?>> InvokeTaskFromClientSerializableClassIReadOnlyCollectionFixed(int version);

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionFixedTask))]
            public partial RpcTask<IReadOnlyCollection<SerializableTypeStructFixed?>> InvokeTaskFromServerSerializableClassIReadOnlyCollectionFixed(int version, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private IReadOnlyCollection<SerializableTypeStructFixed?> ReceiveSerializableClassIReadOnlyCollectionFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructFixedIReadOnlyCollection[version];
            }

            [RpcReceive]
            private async Task<IReadOnlyCollection<SerializableTypeStructFixed?>> ReceiveSerializableClassIReadOnlyCollectionFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructFixedIReadOnlyCollection[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableFixed))]
            public partial RpcTask<IEnumerable<SerializableTypeStructFixed?>> InvokeFromClientSerializableClassIEnumerableFixed(int version);

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableFixed))]
            public partial RpcTask<IEnumerable<SerializableTypeStructFixed?>> InvokeFromServerSerializableClassIEnumerableFixed(int version, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableFixedTask))]
            public partial RpcTask<IEnumerable<SerializableTypeStructFixed?>> InvokeTaskFromClientSerializableClassIEnumerableFixed(int version);

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableFixedTask))]
            public partial RpcTask<IEnumerable<SerializableTypeStructFixed?>> InvokeTaskFromServerSerializableClassIEnumerableFixed(int version, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private IEnumerable<SerializableTypeStructFixed?> ReceiveSerializableClassIEnumerableFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructFixedIEnumerable[version];
            }

            [RpcReceive]
            private async Task<IEnumerable<SerializableTypeStructFixed?>> ReceiveSerializableClassIEnumerableFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructFixedIEnumerable[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassListFixed))]
            public partial RpcTask<List<SerializableTypeStructFixed?>> InvokeFromClientSerializableClassListFixed(int version);

            [RpcSend(nameof(ReceiveSerializableClassListFixed))]
            public partial RpcTask<List<SerializableTypeStructFixed?>> InvokeFromServerSerializableClassListFixed(int version, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableClassListFixedTask))]
            public partial RpcTask<List<SerializableTypeStructFixed?>> InvokeTaskFromClientSerializableClassListFixed(int version);

            [RpcSend(nameof(ReceiveSerializableClassListFixedTask))]
            public partial RpcTask<List<SerializableTypeStructFixed?>> InvokeTaskFromServerSerializableClassListFixed(int version, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private List<SerializableTypeStructFixed?> ReceiveSerializableClassListFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructFixedList[version];
            }

            [RpcReceive]
            private async Task<List<SerializableTypeStructFixed?>> ReceiveSerializableClassListFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructFixedList[version];
            }


            [RpcSend(nameof(ReceiveSerializableClassArrayVariable))]
            public partial RpcTask<SerializableTypeStructVariable?[]> InvokeFromClientSerializableClassArrayVariable(int version);

            [RpcSend(nameof(ReceiveSerializableClassArrayVariable))]
            public partial RpcTask<SerializableTypeStructVariable?[]> InvokeFromServerSerializableClassArrayVariable(int version, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableClassArrayVariableTask))]
            public partial RpcTask<SerializableTypeStructVariable?[]> InvokeTaskFromClientSerializableClassArrayVariable(int version);

            [RpcSend(nameof(ReceiveSerializableClassArrayVariableTask))]
            public partial RpcTask<SerializableTypeStructVariable?[]> InvokeTaskFromServerSerializableClassArrayVariable(int version, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private SerializableTypeStructVariable?[] ReceiveSerializableClassArrayVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructVariableArray[version];
            }

            [RpcReceive]
            private async Task<SerializableTypeStructVariable?[]> ReceiveSerializableClassArrayVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructVariableArray[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentVariable))]
            public partial RpcTask<ArraySegment<SerializableTypeStructVariable?>> InvokeFromClientSerializableClassArraySegmentVariable(int version);

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentVariable))]
            public partial RpcTask<ArraySegment<SerializableTypeStructVariable?>> InvokeFromServerSerializableClassArraySegmentVariable(int version, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentVariableTask))]
            public partial RpcTask<ArraySegment<SerializableTypeStructVariable?>> InvokeTaskFromClientSerializableClassArraySegmentVariable(int version);

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentVariableTask))]
            public partial RpcTask<ArraySegment<SerializableTypeStructVariable?>> InvokeTaskFromServerSerializableClassArraySegmentVariable(int version, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private ArraySegment<SerializableTypeStructVariable?> ReceiveSerializableClassArraySegmentVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructVariableArraySegment[version];
            }

            [RpcReceive]
            private async Task<ArraySegment<SerializableTypeStructVariable?>> ReceiveSerializableClassArraySegmentVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructVariableArraySegment[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassICollectionVariable))]
            public partial RpcTask<ICollection<SerializableTypeStructVariable?>> InvokeFromClientSerializableClassICollectionVariable(int version);

            [RpcSend(nameof(ReceiveSerializableClassICollectionVariable))]
            public partial RpcTask<ICollection<SerializableTypeStructVariable?>> InvokeFromServerSerializableClassICollectionVariable(int version, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableClassICollectionVariableTask))]
            public partial RpcTask<ICollection<SerializableTypeStructVariable?>> InvokeTaskFromClientSerializableClassICollectionVariable(int version);

            [RpcSend(nameof(ReceiveSerializableClassICollectionVariableTask))]
            public partial RpcTask<ICollection<SerializableTypeStructVariable?>> InvokeTaskFromServerSerializableClassICollectionVariable(int version, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private ICollection<SerializableTypeStructVariable?> ReceiveSerializableClassICollectionVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructVariableICollection[version];
            }

            [RpcReceive]
            private async Task<ICollection<SerializableTypeStructVariable?>> ReceiveSerializableClassICollectionVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructVariableICollection[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionVariable))]
            public partial RpcTask<IReadOnlyCollection<SerializableTypeStructVariable?>> InvokeFromClientSerializableClassIReadOnlyCollectionVariable(int version);

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionVariable))]
            public partial RpcTask<IReadOnlyCollection<SerializableTypeStructVariable?>> InvokeFromServerSerializableClassIReadOnlyCollectionVariable(int version, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionVariableTask))]
            public partial RpcTask<IReadOnlyCollection<SerializableTypeStructVariable?>> InvokeTaskFromClientSerializableClassIReadOnlyCollectionVariable(int version);

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionVariableTask))]
            public partial RpcTask<IReadOnlyCollection<SerializableTypeStructVariable?>> InvokeTaskFromServerSerializableClassIReadOnlyCollectionVariable(int version, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private IReadOnlyCollection<SerializableTypeStructVariable?> ReceiveSerializableClassIReadOnlyCollectionVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructVariableIReadOnlyCollection[version];
            }

            [RpcReceive]
            private async Task<IReadOnlyCollection<SerializableTypeStructVariable?>> ReceiveSerializableClassIReadOnlyCollectionVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructVariableIReadOnlyCollection[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableVariable))]
            public partial RpcTask<IEnumerable<SerializableTypeStructVariable?>> InvokeFromClientSerializableClassIEnumerableVariable(int version);

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableVariable))]
            public partial RpcTask<IEnumerable<SerializableTypeStructVariable?>> InvokeFromServerSerializableClassIEnumerableVariable(int version, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableVariableTask))]
            public partial RpcTask<IEnumerable<SerializableTypeStructVariable?>> InvokeTaskFromClientSerializableClassIEnumerableVariable(int version);

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableVariableTask))]
            public partial RpcTask<IEnumerable<SerializableTypeStructVariable?>> InvokeTaskFromServerSerializableClassIEnumerableVariable(int version, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private IEnumerable<SerializableTypeStructVariable?> ReceiveSerializableClassIEnumerableVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructVariableIEnumerable[version];
            }

            [RpcReceive]
            private async Task<IEnumerable<SerializableTypeStructVariable?>> ReceiveSerializableClassIEnumerableVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructVariableIEnumerable[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassListVariable))]
            public partial RpcTask<List<SerializableTypeStructVariable?>> InvokeFromClientSerializableClassListVariable(int version);

            [RpcSend(nameof(ReceiveSerializableClassListVariable))]
            public partial RpcTask<List<SerializableTypeStructVariable?>> InvokeFromServerSerializableClassListVariable(int version, IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveSerializableClassListVariableTask))]
            public partial RpcTask<List<SerializableTypeStructVariable?>> InvokeTaskFromClientSerializableClassListVariable(int version);

            [RpcSend(nameof(ReceiveSerializableClassListVariableTask))]
            public partial RpcTask<List<SerializableTypeStructVariable?>> InvokeTaskFromServerSerializableClassListVariable(int version, IModularRpcRemoteConnection connection);

            [RpcReceive]
            private List<SerializableTypeStructVariable?> ReceiveSerializableClassListVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructVariableList[version];
            }

            [RpcReceive]
            private async Task<List<SerializableTypeStructVariable?>> ReceiveSerializableClassListVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructVariableList[version];
            }
        }
    }
}