using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModularRPCs.Test.CodeGen
{
    [NonParallelizable, TestFixture]
    public class ReturnTypeSerializableCollection
    {
        private static bool _wasInvoked;

        private const int RtnValueSerializableTypeClassFixedArrayLength = 6;
        private static readonly SerializableTypeClassFixed[][] RtnValueSerializableTypeClassFixedArray =
            new[]
            {
                new SerializableTypeClassFixed[]
                {
                    new SerializableTypeClassFixed
                    {
                        Value = 1
                    },
                    new SerializableTypeClassFixed
                    {
                        Value = 2
                    }
                },
                null,
                new SerializableTypeClassFixed[0],
                new SerializableTypeClassFixed[]
                {
                    new SerializableTypeClassFixed
                    {
                        Value = 1
                    }
                },
                new SerializableTypeClassFixed[]
                {
                    null
                },
                new SerializableTypeClassFixed[]
                {
                    new SerializableTypeClassFixed
                    {
                        Value = 1
                    },
                    null,
                    new SerializableTypeClassFixed
                    {
                        Value = 999
                    }
                },
            };

        private const int RtnValueSerializableTypeClassFixedArraySegmentLength = 10;
        private static readonly ArraySegment<SerializableTypeClassFixed>[] RtnValueSerializableTypeClassFixedArraySegment =
            new[]
            {
                new ArraySegment<SerializableTypeClassFixed>(new SerializableTypeClassFixed[]
                {
                    new SerializableTypeClassFixed
                    {
                        Value = 1
                    },
                    new SerializableTypeClassFixed
                    {
                        Value = 2
                    }
                }),
                default,
                new ArraySegment<SerializableTypeClassFixed>(new SerializableTypeClassFixed[0]),
                new ArraySegment<SerializableTypeClassFixed>(new SerializableTypeClassFixed[]
                {
                    new SerializableTypeClassFixed
                    {
                        Value = 1
                    }
                }),
                new ArraySegment<SerializableTypeClassFixed>(new SerializableTypeClassFixed[]
                {
                    null
                }),
                new ArraySegment<SerializableTypeClassFixed>(new SerializableTypeClassFixed[]
                {
                    new SerializableTypeClassFixed
                    {
                        Value = 1
                    },
                    null,
                    new SerializableTypeClassFixed
                    {
                        Value = 999
                    }
                }),
                new ArraySegment<SerializableTypeClassFixed>(new SerializableTypeClassFixed[]
                {
                    new SerializableTypeClassFixed
                    {
                        Value = 1
                    },
                    new SerializableTypeClassFixed
                    {
                        Value = 2
                    },
                    new SerializableTypeClassFixed
                    {
                        Value = 3
                    },
                    new SerializableTypeClassFixed
                    {
                        Value = 4
                    }
                }, 1, 2),
                new ArraySegment<SerializableTypeClassFixed>(new SerializableTypeClassFixed[]
                {
                    new SerializableTypeClassFixed
                    {
                        Value = 1
                    }
                }, 0, 0),
                new ArraySegment<SerializableTypeClassFixed>(new SerializableTypeClassFixed[]
                {
                    null,
                    null,
                    null
                }, 1, 0),
                new ArraySegment<SerializableTypeClassFixed>(new SerializableTypeClassFixed[]
                {
                    new SerializableTypeClassFixed
                    {
                        Value = 1
                    },
                    null,
                    new SerializableTypeClassFixed
                    {
                        Value = 999
                    }
                }, 0, 2),
            };

        private const int RtnValueSerializableTypeClassFixedICollectionLength = RtnValueSerializableTypeClassFixedArrayLength;
        private static readonly CollectionImpl<SerializableTypeClassFixed>[] RtnValueSerializableTypeClassFixedICollection =
            RtnValueSerializableTypeClassFixedArray.Select(x => x == null ? null : new CollectionImpl<SerializableTypeClassFixed>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeClassFixedIReadOnlyCollectionLength = RtnValueSerializableTypeClassFixedArrayLength;
        private static readonly ReadOnlyCollectionImpl<SerializableTypeClassFixed>[] RtnValueSerializableTypeClassFixedIReadOnlyCollection =
            RtnValueSerializableTypeClassFixedArray.Select(x => x == null ? null : new ReadOnlyCollectionImpl<SerializableTypeClassFixed>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeClassFixedIEnumerableLength = RtnValueSerializableTypeClassFixedArrayLength;
        private static readonly EnumerableImpl<SerializableTypeClassFixed>[] RtnValueSerializableTypeClassFixedIEnumerable =
            RtnValueSerializableTypeClassFixedArray.Select(x => x == null ? null : new EnumerableImpl<SerializableTypeClassFixed>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeClassFixedListLength = RtnValueSerializableTypeClassFixedArrayLength;
        private static readonly List<SerializableTypeClassFixed>[] RtnValueSerializableTypeClassFixedList =
            RtnValueSerializableTypeClassFixedArray.Select(x => x == null ? null : new List<SerializableTypeClassFixed>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeClassVariableArrayLength = 6;
        private static readonly SerializableTypeClassVariable[][] RtnValueSerializableTypeClassVariableArray =
            new[]
            {
                new SerializableTypeClassVariable[]
                {
                    new SerializableTypeClassVariable
                    {
                        Value = "te1"
                    },
                    new SerializableTypeClassVariable
                    {
                        Value = "te2"
                    }
                },
                null,
                new SerializableTypeClassVariable[0],
                new SerializableTypeClassVariable[]
                {
                    new SerializableTypeClassVariable
                    {
                        Value = "test 1"
                    }
                },
                new SerializableTypeClassVariable[]
                {
                    null
                },
                new SerializableTypeClassVariable[]
                {
                    new SerializableTypeClassVariable
                    {
                        Value = "v 1"
                    },
                    null,
                    new SerializableTypeClassVariable
                    {
                        Value = "avakhfbkhsfkshf"
                    }
                },
            };

        private const int RtnValueSerializableTypeClassVariableArraySegmentLength = 10;
        private static readonly ArraySegment<SerializableTypeClassVariable>[] RtnValueSerializableTypeClassVariableArraySegment =
            new[]
            {
                new ArraySegment<SerializableTypeClassVariable>(new SerializableTypeClassVariable[]
                {
                    new SerializableTypeClassVariable
                    {
                        Value = "afasfshf "
                    },
                    new SerializableTypeClassVariable
                    {
                        Value = " aifj "
                    }
                }),
                default,
                new ArraySegment<SerializableTypeClassVariable>(new SerializableTypeClassVariable[0]),
                new ArraySegment<SerializableTypeClassVariable>(new SerializableTypeClassVariable[]
                {
                    new SerializableTypeClassVariable
                    {
                        Value = " v 1 "
                    }
                }),
                new ArraySegment<SerializableTypeClassVariable>(new SerializableTypeClassVariable[]
                {
                    null
                }),
                new ArraySegment<SerializableTypeClassVariable>(new SerializableTypeClassVariable[]
                {
                    new SerializableTypeClassVariable
                    {
                        Value = ""
                    },
                    null,
                    new SerializableTypeClassVariable
                    {
                        Value = "V e8f"
                    }
                }),
                new ArraySegment<SerializableTypeClassVariable>(new SerializableTypeClassVariable[]
                {
                    new SerializableTypeClassVariable
                    {
                        Value = " "
                    },
                    new SerializableTypeClassVariable
                    {
                        Value = "lmao "
                    },
                    new SerializableTypeClassVariable
                    {
                        Value = "no "
                    },
                    new SerializableTypeClassVariable
                    {
                        Value = "v4"
                    }
                }, 1, 2),
                new ArraySegment<SerializableTypeClassVariable>(new SerializableTypeClassVariable[]
                {
                    new SerializableTypeClassVariable
                    {
                        Value = "text "
                    }
                }, 0, 0),
                new ArraySegment<SerializableTypeClassVariable>(new SerializableTypeClassVariable[]
                {
                    null,
                    null,
                    null
                }, 1, 0),
                new ArraySegment<SerializableTypeClassVariable>(new SerializableTypeClassVariable[]
                {
                    new SerializableTypeClassVariable
                    {
                        Value = " txt"
                    },
                    null,
                    new SerializableTypeClassVariable
                    {
                        Value = " t  e  x t "
                    }
                }, 0, 2),
            };

        private const int RtnValueSerializableTypeClassVariableICollectionLength = RtnValueSerializableTypeClassVariableArrayLength;
        private static readonly CollectionImpl<SerializableTypeClassVariable>[] RtnValueSerializableTypeClassVariableICollection =
            RtnValueSerializableTypeClassVariableArray.Select(x => x == null ? null : new CollectionImpl<SerializableTypeClassVariable>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeClassVariableIReadOnlyCollectionLength = RtnValueSerializableTypeClassVariableArrayLength;
        private static readonly ReadOnlyCollectionImpl<SerializableTypeClassVariable>[] RtnValueSerializableTypeClassVariableIReadOnlyCollection =
            RtnValueSerializableTypeClassVariableArray.Select(x => x == null ? null : new ReadOnlyCollectionImpl<SerializableTypeClassVariable>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeClassVariableIEnumerableLength = RtnValueSerializableTypeClassVariableArrayLength;
        private static readonly EnumerableImpl<SerializableTypeClassVariable>[] RtnValueSerializableTypeClassVariableIEnumerable =
            RtnValueSerializableTypeClassVariableArray.Select(x => x == null ? null : new EnumerableImpl<SerializableTypeClassVariable>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeClassVariableListLength = RtnValueSerializableTypeClassVariableArrayLength;
        private static readonly List<SerializableTypeClassVariable>[] RtnValueSerializableTypeClassVariableList =
            RtnValueSerializableTypeClassVariableArray.Select(x => x == null ? null : new List<SerializableTypeClassVariable>(x.ToList())).ToArray();


        private const int RtnValueSerializableTypeStructFixedArrayLength = 4;
        private static readonly SerializableTypeStructFixed[][] RtnValueSerializableTypeStructFixedArray =
            new[]
            {
                new SerializableTypeStructFixed[]
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
                new SerializableTypeStructFixed[0],
                new SerializableTypeStructFixed[]
                {
                    new SerializableTypeStructFixed
                    {
                        Value = 1
                    }
                }
            };

        private const int RtnValueSerializableTypeStructFixedArraySegmentLength = 6;
        private static readonly ArraySegment<SerializableTypeStructFixed>[] RtnValueSerializableTypeStructFixedArraySegment =
            new[]
            {
                new ArraySegment<SerializableTypeStructFixed>(new SerializableTypeStructFixed[]
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
                new ArraySegment<SerializableTypeStructFixed>(new SerializableTypeStructFixed[0]),
                new ArraySegment<SerializableTypeStructFixed>(new SerializableTypeStructFixed[]
                {
                    new SerializableTypeStructFixed
                    {
                        Value = 1
                    }
                }),
                new ArraySegment<SerializableTypeStructFixed>(new SerializableTypeStructFixed[]
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
                new ArraySegment<SerializableTypeStructFixed>(new SerializableTypeStructFixed[]
                {
                    new SerializableTypeStructFixed
                    {
                        Value = 1
                    }
                }, 0, 0),
            };

        private const int RtnValueSerializableTypeStructFixedICollectionLength = RtnValueSerializableTypeStructFixedArrayLength;
        private static readonly CollectionImpl<SerializableTypeStructFixed>[] RtnValueSerializableTypeStructFixedICollection =
            RtnValueSerializableTypeStructFixedArray.Select(x => x == null ? null : new CollectionImpl<SerializableTypeStructFixed>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructFixedIReadOnlyCollectionLength = RtnValueSerializableTypeStructFixedArrayLength;
        private static readonly ReadOnlyCollectionImpl<SerializableTypeStructFixed>[] RtnValueSerializableTypeStructFixedIReadOnlyCollection =
            RtnValueSerializableTypeStructFixedArray.Select(x => x == null ? null : new ReadOnlyCollectionImpl<SerializableTypeStructFixed>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructFixedIEnumerableLength = RtnValueSerializableTypeStructFixedArrayLength;
        private static readonly EnumerableImpl<SerializableTypeStructFixed>[] RtnValueSerializableTypeStructFixedIEnumerable =
            RtnValueSerializableTypeStructFixedArray.Select(x => x == null ? null : new EnumerableImpl<SerializableTypeStructFixed>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructFixedListLength = RtnValueSerializableTypeStructFixedArrayLength;
        private static readonly List<SerializableTypeStructFixed>[] RtnValueSerializableTypeStructFixedList =
            RtnValueSerializableTypeStructFixedArray.Select(x => x == null ? null : new List<SerializableTypeStructFixed>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructVariableArrayLength = 4;
        private static readonly SerializableTypeStructVariable[][] RtnValueSerializableTypeStructVariableArray =
            new[]
            {
                new SerializableTypeStructVariable[]
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
                new SerializableTypeStructVariable[0],
                new SerializableTypeStructVariable[]
                {
                    new SerializableTypeStructVariable
                    {
                        Value = "test 1"
                    }
                }
            };

        private const int RtnValueSerializableTypeStructVariableArraySegmentLength = 6;
        private static readonly ArraySegment<SerializableTypeStructVariable>[] RtnValueSerializableTypeStructVariableArraySegment =
            new[]
            {
                new ArraySegment<SerializableTypeStructVariable>(new SerializableTypeStructVariable[]
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
                new ArraySegment<SerializableTypeStructVariable>(new SerializableTypeStructVariable[0]),
                new ArraySegment<SerializableTypeStructVariable>(new SerializableTypeStructVariable[]
                {
                    new SerializableTypeStructVariable
                    {
                        Value = " v 1 "
                    }
                }),
                new ArraySegment<SerializableTypeStructVariable>(new SerializableTypeStructVariable[]
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
                new ArraySegment<SerializableTypeStructVariable>(new SerializableTypeStructVariable[]
                {
                    new SerializableTypeStructVariable
                    {
                        Value = "text "
                    }
                }, 0, 0)
            };

        private const int RtnValueSerializableTypeStructVariableICollectionLength = RtnValueSerializableTypeStructVariableArrayLength;
        private static readonly CollectionImpl<SerializableTypeStructVariable>[] RtnValueSerializableTypeStructVariableICollection =
            RtnValueSerializableTypeStructVariableArray.Select(x => x == null ? null : new CollectionImpl<SerializableTypeStructVariable>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructVariableIReadOnlyCollectionLength = RtnValueSerializableTypeStructVariableArrayLength;
        private static readonly ReadOnlyCollectionImpl<SerializableTypeStructVariable>[] RtnValueSerializableTypeStructVariableIReadOnlyCollection =
            RtnValueSerializableTypeStructVariableArray.Select(x => x == null ? null : new ReadOnlyCollectionImpl<SerializableTypeStructVariable>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructVariableIEnumerableLength = RtnValueSerializableTypeStructVariableArrayLength;
        private static readonly EnumerableImpl<SerializableTypeStructVariable>[] RtnValueSerializableTypeStructVariableIEnumerable =
            RtnValueSerializableTypeStructVariableArray.Select(x => x == null ? null : new EnumerableImpl<SerializableTypeStructVariable>(x.ToList())).ToArray();

        private const int RtnValueSerializableTypeStructVariableListLength = RtnValueSerializableTypeStructVariableArrayLength;
        private static readonly List<SerializableTypeStructVariable>[] RtnValueSerializableTypeStructVariableList =
            RtnValueSerializableTypeStructVariableArray.Select(x => x == null ? null : new List<SerializableTypeStructVariable>(x.ToList())).ToArray();

        [Test]
        public async Task ServerToClientBytesSerializableTypeClassArrayFixed([Range(0, RtnValueSerializableTypeClassFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassFixed[] rtnValue = await proxy.InvokeFromServerSerializableClassArrayFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArray[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassArrayFixed([Range(0, RtnValueSerializableTypeClassFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassFixed[] rtnValue = await proxy.InvokeFromClientSerializableClassArrayFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArray[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassArrayFixed([Range(0, RtnValueSerializableTypeClassFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassFixed[] rtnValue = await proxy.InvokeFromServerSerializableClassArrayFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArray[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassArrayFixed([Range(0, RtnValueSerializableTypeClassFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassFixed[] rtnValue = await proxy.InvokeFromClientSerializableClassArrayFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArray[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassArrayFixed([Range(0, RtnValueSerializableTypeClassFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassFixed[] rtnValue = await proxy.InvokeTaskFromServerSerializableClassArrayFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArray[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassArrayFixed([Range(0, RtnValueSerializableTypeClassFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassFixed[] rtnValue = await proxy.InvokeTaskFromClientSerializableClassArrayFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArray[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassArrayFixed([Range(0, RtnValueSerializableTypeClassFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassFixed[] rtnValue = await proxy.InvokeTaskFromServerSerializableClassArrayFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArray[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassArrayFixed([Range(0, RtnValueSerializableTypeClassFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassFixed[] rtnValue = await proxy.InvokeTaskFromClientSerializableClassArrayFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArray[version]));
        }



        [Test]
        public async Task ServerToClientBytesSerializableTypeClassArraySegmentFixed([Range(0, RtnValueSerializableTypeClassFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromServerSerializableClassArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassArraySegmentFixed([Range(0, RtnValueSerializableTypeClassFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromClientSerializableClassArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArraySegment[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassArraySegmentFixed([Range(0, RtnValueSerializableTypeClassFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromServerSerializableClassArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassArraySegmentFixed([Range(0, RtnValueSerializableTypeClassFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromClientSerializableClassArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArraySegment[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassArraySegmentFixed([Range(0, RtnValueSerializableTypeClassFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableClassArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassArraySegmentFixed([Range(0, RtnValueSerializableTypeClassFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableClassArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArraySegment[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassArraySegmentFixed([Range(0, RtnValueSerializableTypeClassFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableClassArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassArraySegmentFixed([Range(0, RtnValueSerializableTypeClassFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableClassArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArraySegment[version]));
        }



        [Test]
        public async Task ServerToClientBytesSerializableTypeClassICollectionFixed([Range(0, RtnValueSerializableTypeClassFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromServerSerializableClassICollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassICollectionFixed([Range(0, RtnValueSerializableTypeClassFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromClientSerializableClassICollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedICollection[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassICollectionFixed([Range(0, RtnValueSerializableTypeClassFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromServerSerializableClassICollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassICollectionFixed([Range(0, RtnValueSerializableTypeClassFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromClientSerializableClassICollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedICollection[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassICollectionFixed([Range(0, RtnValueSerializableTypeClassFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableClassICollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassICollectionFixed([Range(0, RtnValueSerializableTypeClassFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableClassICollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedICollection[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassICollectionFixed([Range(0, RtnValueSerializableTypeClassFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableClassICollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassICollectionFixed([Range(0, RtnValueSerializableTypeClassFixedICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableClassICollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedICollection[version]));
        }




        [Test]
        public async Task ServerToClientBytesSerializableTypeClassIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeClassFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromServerSerializableClassIReadOnlyCollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeClassFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromClientSerializableClassIReadOnlyCollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeClassFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromServerSerializableClassIReadOnlyCollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeClassFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromClientSerializableClassIReadOnlyCollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeClassFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIReadOnlyCollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeClassFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIReadOnlyCollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeClassFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIReadOnlyCollectionFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassIReadOnlyCollectionFixed([Range(0, RtnValueSerializableTypeClassFixedIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIReadOnlyCollectionFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIReadOnlyCollection[version]));
        }




        [Test]
        public async Task ServerToClientBytesSerializableTypeClassIEnumerableFixed([Range(0, RtnValueSerializableTypeClassFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromServerSerializableClassIEnumerableFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassIEnumerableFixed([Range(0, RtnValueSerializableTypeClassFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromClientSerializableClassIEnumerableFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIEnumerable[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassIEnumerableFixed([Range(0, RtnValueSerializableTypeClassFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromServerSerializableClassIEnumerableFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassIEnumerableFixed([Range(0, RtnValueSerializableTypeClassFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromClientSerializableClassIEnumerableFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIEnumerable[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassIEnumerableFixed([Range(0, RtnValueSerializableTypeClassFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIEnumerableFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassIEnumerableFixed([Range(0, RtnValueSerializableTypeClassFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIEnumerableFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIEnumerable[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassIEnumerableFixed([Range(0, RtnValueSerializableTypeClassFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIEnumerableFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassIEnumerableFixed([Range(0, RtnValueSerializableTypeClassFixedIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIEnumerableFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedIEnumerable[version]));
        }




        [Test]
        public async Task ServerToClientBytesSerializableTypeClassListFixed([Range(0, RtnValueSerializableTypeClassFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromServerSerializableClassListFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedList[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassListFixed([Range(0, RtnValueSerializableTypeClassFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromClientSerializableClassListFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedList[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassListFixed([Range(0, RtnValueSerializableTypeClassFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromServerSerializableClassListFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedList[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassListFixed([Range(0, RtnValueSerializableTypeClassFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeClassFixed> rtnValue = await proxy.InvokeFromClientSerializableClassListFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedList[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassListFixed([Range(0, RtnValueSerializableTypeClassFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableClassListFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedList[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassListFixed([Range(0, RtnValueSerializableTypeClassFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableClassListFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedList[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassListFixed([Range(0, RtnValueSerializableTypeClassFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableClassListFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedList[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassListFixed([Range(0, RtnValueSerializableTypeClassFixedListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeClassFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableClassListFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassFixedList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedList[version]));
        }





        [Test]
        public async Task ServerToClientBytesSerializableTypeClassArrayVariable([Range(0, RtnValueSerializableTypeClassVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassVariable[] rtnValue = await proxy.InvokeFromServerSerializableClassArrayVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArray[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassArrayVariable([Range(0, RtnValueSerializableTypeClassVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassVariable[] rtnValue = await proxy.InvokeFromClientSerializableClassArrayVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArray[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassArrayVariable([Range(0, RtnValueSerializableTypeClassVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassVariable[] rtnValue = await proxy.InvokeFromServerSerializableClassArrayVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArray[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassArrayVariable([Range(0, RtnValueSerializableTypeClassVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassVariable[] rtnValue = await proxy.InvokeFromClientSerializableClassArrayVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArray[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassArrayVariable([Range(0, RtnValueSerializableTypeClassVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassVariable[] rtnValue = await proxy.InvokeTaskFromServerSerializableClassArrayVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArray[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassArrayVariable([Range(0, RtnValueSerializableTypeClassVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassVariable[] rtnValue = await proxy.InvokeTaskFromClientSerializableClassArrayVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArray[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassArrayVariable([Range(0, RtnValueSerializableTypeClassVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeClassVariable[] rtnValue = await proxy.InvokeTaskFromServerSerializableClassArrayVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArray[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassArrayVariable([Range(0, RtnValueSerializableTypeClassVariableArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            SerializableTypeClassVariable[] rtnValue = await proxy.InvokeTaskFromClientSerializableClassArrayVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableArray[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArray[version]));
        }



        [Test]
        public async Task ServerToClientBytesSerializableTypeClassArraySegmentVariable([Range(0, RtnValueSerializableTypeClassVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromServerSerializableClassArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassArraySegmentVariable([Range(0, RtnValueSerializableTypeClassVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromClientSerializableClassArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArraySegment[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassArraySegmentVariable([Range(0, RtnValueSerializableTypeClassVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromServerSerializableClassArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassArraySegmentVariable([Range(0, RtnValueSerializableTypeClassVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromClientSerializableClassArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArraySegment[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassArraySegmentVariable([Range(0, RtnValueSerializableTypeClassVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableClassArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassArraySegmentVariable([Range(0, RtnValueSerializableTypeClassVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableClassArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArraySegment[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassArraySegmentVariable([Range(0, RtnValueSerializableTypeClassVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableClassArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassArraySegmentVariable([Range(0, RtnValueSerializableTypeClassVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableClassArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue.Array == null)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Array, Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArraySegment[version]));
        }



        [Test]
        public async Task ServerToClientBytesSerializableTypeClassICollectionVariable([Range(0, RtnValueSerializableTypeClassVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromServerSerializableClassICollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassICollectionVariable([Range(0, RtnValueSerializableTypeClassVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromClientSerializableClassICollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableICollection[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassICollectionVariable([Range(0, RtnValueSerializableTypeClassVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromServerSerializableClassICollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassICollectionVariable([Range(0, RtnValueSerializableTypeClassVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromClientSerializableClassICollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableICollection[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassICollectionVariable([Range(0, RtnValueSerializableTypeClassVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableClassICollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassICollectionVariable([Range(0, RtnValueSerializableTypeClassVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableClassICollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableICollection[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassICollectionVariable([Range(0, RtnValueSerializableTypeClassVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableClassICollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableICollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassICollectionVariable([Range(0, RtnValueSerializableTypeClassVariableICollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ICollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableClassICollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableICollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableICollection[version]));
        }




        [Test]
        public async Task ServerToClientBytesSerializableTypeClassIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeClassVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromServerSerializableClassIReadOnlyCollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeClassVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromClientSerializableClassIReadOnlyCollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeClassVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromServerSerializableClassIReadOnlyCollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeClassVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromClientSerializableClassIReadOnlyCollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeClassVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIReadOnlyCollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeClassVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIReadOnlyCollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeClassVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIReadOnlyCollectionVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassIReadOnlyCollectionVariable([Range(0, RtnValueSerializableTypeClassVariableIReadOnlyCollectionLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IReadOnlyCollection<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIReadOnlyCollectionVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIReadOnlyCollection[version]));
        }




        [Test]
        public async Task ServerToClientBytesSerializableTypeClassIEnumerableVariable([Range(0, RtnValueSerializableTypeClassVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromServerSerializableClassIEnumerableVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassIEnumerableVariable([Range(0, RtnValueSerializableTypeClassVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromClientSerializableClassIEnumerableVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIEnumerable[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassIEnumerableVariable([Range(0, RtnValueSerializableTypeClassVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromServerSerializableClassIEnumerableVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassIEnumerableVariable([Range(0, RtnValueSerializableTypeClassVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromClientSerializableClassIEnumerableVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIEnumerable[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassIEnumerableVariable([Range(0, RtnValueSerializableTypeClassVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIEnumerableVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassIEnumerableVariable([Range(0, RtnValueSerializableTypeClassVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIEnumerableVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIEnumerable[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassIEnumerableVariable([Range(0, RtnValueSerializableTypeClassVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableClassIEnumerableVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIEnumerable[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassIEnumerableVariable([Range(0, RtnValueSerializableTypeClassVariableIEnumerableLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            IEnumerable<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableClassIEnumerableVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableIEnumerable[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableIEnumerable[version]));
        }




        [Test]
        public async Task ServerToClientBytesSerializableTypeClassListVariable([Range(0, RtnValueSerializableTypeClassVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromServerSerializableClassListVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableList[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassListVariable([Range(0, RtnValueSerializableTypeClassVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromClientSerializableClassListVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableList[version]));
        }

        [Test]
        public async Task ServerToClientStreamSerializableTypeClassListVariable([Range(0, RtnValueSerializableTypeClassVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromServerSerializableClassListVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableList[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassListVariable([Range(0, RtnValueSerializableTypeClassVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeClassVariable> rtnValue = await proxy.InvokeFromClientSerializableClassListVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableList[version]));
        }


        [Test]
        public async Task ServerToClientTaskBytesSerializableTypeClassListVariable([Range(0, RtnValueSerializableTypeClassVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableClassListVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableList[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassListVariable([Range(0, RtnValueSerializableTypeClassVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableClassListVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableList[version]));
        }

        [Test]
        public async Task ServerToClientTaskStreamSerializableTypeClassListVariable([Range(0, RtnValueSerializableTypeClassVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            List<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableClassListVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableList[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassListVariable([Range(0, RtnValueSerializableTypeClassVariableListLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            List<SerializableTypeClassVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableClassListVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeClassVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableList[version]));
        }

        [Test]
        public async Task ServerToClientBytesSerializableTypeStructArrayFixed([Range(0, RtnValueSerializableTypeStructFixedArrayLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            SerializableTypeStructFixed[] rtnValue = await proxy.InvokeFromServerSerializableStructArrayFixed(version, connection);

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

            SerializableTypeStructFixed[] rtnValue = await proxy.InvokeFromClientSerializableStructArrayFixed(version);

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

            SerializableTypeStructFixed[] rtnValue = await proxy.InvokeFromServerSerializableStructArrayFixed(version, connection);

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

            SerializableTypeStructFixed[] rtnValue = await proxy.InvokeFromClientSerializableStructArrayFixed(version);

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

            SerializableTypeStructFixed[] rtnValue = await proxy.InvokeTaskFromServerSerializableStructArrayFixed(version, connection);

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

            SerializableTypeStructFixed[] rtnValue = await proxy.InvokeTaskFromClientSerializableStructArrayFixed(version);

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

            SerializableTypeStructFixed[] rtnValue = await proxy.InvokeTaskFromServerSerializableStructArrayFixed(version, connection);

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

            SerializableTypeStructFixed[] rtnValue = await proxy.InvokeTaskFromClientSerializableStructArrayFixed(version);

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

            ArraySegment<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromServerSerializableStructArraySegmentFixed(version, connection);

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

            ArraySegment<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromClientSerializableStructArraySegmentFixed(version);

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

            ArraySegment<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromServerSerializableStructArraySegmentFixed(version, connection);

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

            ArraySegment<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromClientSerializableStructArraySegmentFixed(version);

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

            ArraySegment<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableStructArraySegmentFixed(version, connection);

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

            ArraySegment<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableStructArraySegmentFixed(version);

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

            ArraySegment<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableStructArraySegmentFixed(version, connection);

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

            ArraySegment<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableStructArraySegmentFixed(version);

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

            ICollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromServerSerializableStructICollectionFixed(version, connection);

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

            ICollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromClientSerializableStructICollectionFixed(version);

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

            ICollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromServerSerializableStructICollectionFixed(version, connection);

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

            ICollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromClientSerializableStructICollectionFixed(version);

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

            ICollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableStructICollectionFixed(version, connection);

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

            ICollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableStructICollectionFixed(version);

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

            ICollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableStructICollectionFixed(version, connection);

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

            ICollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableStructICollectionFixed(version);

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

            IReadOnlyCollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromServerSerializableStructIReadOnlyCollectionFixed(version, connection);

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

            IReadOnlyCollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromClientSerializableStructIReadOnlyCollectionFixed(version);

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

            IReadOnlyCollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromServerSerializableStructIReadOnlyCollectionFixed(version, connection);

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

            IReadOnlyCollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromClientSerializableStructIReadOnlyCollectionFixed(version);

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

            IReadOnlyCollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableStructIReadOnlyCollectionFixed(version, connection);

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

            IReadOnlyCollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableStructIReadOnlyCollectionFixed(version);

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

            IReadOnlyCollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableStructIReadOnlyCollectionFixed(version, connection);

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

            IReadOnlyCollection<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableStructIReadOnlyCollectionFixed(version);

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

            IEnumerable<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromServerSerializableStructIEnumerableFixed(version, connection);

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

            IEnumerable<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromClientSerializableStructIEnumerableFixed(version);

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

            IEnumerable<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromServerSerializableStructIEnumerableFixed(version, connection);

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

            IEnumerable<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromClientSerializableStructIEnumerableFixed(version);

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

            IEnumerable<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableStructIEnumerableFixed(version, connection);

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

            IEnumerable<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableStructIEnumerableFixed(version);

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

            IEnumerable<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableStructIEnumerableFixed(version, connection);

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

            IEnumerable<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableStructIEnumerableFixed(version);

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

            List<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromServerSerializableStructListFixed(version, connection);

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

            List<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromClientSerializableStructListFixed(version);

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

            List<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromServerSerializableStructListFixed(version, connection);

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

            List<SerializableTypeStructFixed> rtnValue = await proxy.InvokeFromClientSerializableStructListFixed(version);

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

            List<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableStructListFixed(version, connection);

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

            List<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableStructListFixed(version);

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

            List<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromServerSerializableStructListFixed(version, connection);

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

            List<SerializableTypeStructFixed> rtnValue = await proxy.InvokeTaskFromClientSerializableStructListFixed(version);

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

            SerializableTypeStructVariable[] rtnValue = await proxy.InvokeFromServerSerializableStructArrayVariable(version, connection);

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

            SerializableTypeStructVariable[] rtnValue = await proxy.InvokeFromClientSerializableStructArrayVariable(version);

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

            SerializableTypeStructVariable[] rtnValue = await proxy.InvokeFromServerSerializableStructArrayVariable(version, connection);

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

            SerializableTypeStructVariable[] rtnValue = await proxy.InvokeFromClientSerializableStructArrayVariable(version);

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

            SerializableTypeStructVariable[] rtnValue = await proxy.InvokeTaskFromServerSerializableStructArrayVariable(version, connection);

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

            SerializableTypeStructVariable[] rtnValue = await proxy.InvokeTaskFromClientSerializableStructArrayVariable(version);

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

            SerializableTypeStructVariable[] rtnValue = await proxy.InvokeTaskFromServerSerializableStructArrayVariable(version, connection);

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

            SerializableTypeStructVariable[] rtnValue = await proxy.InvokeTaskFromClientSerializableStructArrayVariable(version);

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

            ArraySegment<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromServerSerializableStructArraySegmentVariable(version, connection);

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

            ArraySegment<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromClientSerializableStructArraySegmentVariable(version);

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

            ArraySegment<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromServerSerializableStructArraySegmentVariable(version, connection);

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

            ArraySegment<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromClientSerializableStructArraySegmentVariable(version);

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

            ArraySegment<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableStructArraySegmentVariable(version, connection);

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

            ArraySegment<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableStructArraySegmentVariable(version);

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

            ArraySegment<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableStructArraySegmentVariable(version, connection);

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

            ArraySegment<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableStructArraySegmentVariable(version);

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

            ICollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromServerSerializableStructICollectionVariable(version, connection);

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

            ICollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromClientSerializableStructICollectionVariable(version);

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

            ICollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromServerSerializableStructICollectionVariable(version, connection);

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

            ICollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromClientSerializableStructICollectionVariable(version);

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

            ICollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableStructICollectionVariable(version, connection);

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

            ICollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableStructICollectionVariable(version);

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

            ICollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableStructICollectionVariable(version, connection);

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

            ICollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableStructICollectionVariable(version);

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

            IReadOnlyCollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromServerSerializableStructIReadOnlyCollectionVariable(version, connection);

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

            IReadOnlyCollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromClientSerializableStructIReadOnlyCollectionVariable(version);

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

            IReadOnlyCollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromServerSerializableStructIReadOnlyCollectionVariable(version, connection);

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

            IReadOnlyCollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromClientSerializableStructIReadOnlyCollectionVariable(version);

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

            IReadOnlyCollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableStructIReadOnlyCollectionVariable(version, connection);

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

            IReadOnlyCollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableStructIReadOnlyCollectionVariable(version);

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

            IReadOnlyCollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableStructIReadOnlyCollectionVariable(version, connection);

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

            IReadOnlyCollection<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableStructIReadOnlyCollectionVariable(version);

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

            IEnumerable<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromServerSerializableStructIEnumerableVariable(version, connection);

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

            IEnumerable<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromClientSerializableStructIEnumerableVariable(version);

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

            IEnumerable<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromServerSerializableStructIEnumerableVariable(version, connection);

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

            IEnumerable<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromClientSerializableStructIEnumerableVariable(version);

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

            IEnumerable<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableStructIEnumerableVariable(version, connection);

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

            IEnumerable<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableStructIEnumerableVariable(version);

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

            IEnumerable<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableStructIEnumerableVariable(version, connection);

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

            IEnumerable<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableStructIEnumerableVariable(version);

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

            List<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromServerSerializableStructListVariable(version, connection);

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

            List<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromClientSerializableStructListVariable(version);

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

            List<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromServerSerializableStructListVariable(version, connection);

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

            List<SerializableTypeStructVariable> rtnValue = await proxy.InvokeFromClientSerializableStructListVariable(version);

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

            List<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableStructListVariable(version, connection);

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

            List<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableStructListVariable(version);

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

            List<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromServerSerializableStructListVariable(version, connection);

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

            List<SerializableTypeStructVariable> rtnValue = await proxy.InvokeTaskFromClientSerializableStructListVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (rtnValue == null)
                Assert.That(RtnValueSerializableTypeStructVariableList[version], Is.Null);
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableList[version]));
        }

        [RpcClass]
        public class TestClass
        {
            [RpcSend(nameof(ReceiveSerializableClassArrayFixed))]
            public virtual RpcTask<SerializableTypeClassFixed[]> InvokeFromClientSerializableClassArrayFixed(int version) => RpcTask<SerializableTypeClassFixed[]>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArrayFixed))]
            public virtual RpcTask<SerializableTypeClassFixed[]> InvokeFromServerSerializableClassArrayFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeClassFixed[]>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArrayFixedTask))]
            public virtual RpcTask<SerializableTypeClassFixed[]> InvokeTaskFromClientSerializableClassArrayFixed(int version) => RpcTask<SerializableTypeClassFixed[]>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArrayFixedTask))]
            public virtual RpcTask<SerializableTypeClassFixed[]> InvokeTaskFromServerSerializableClassArrayFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeClassFixed[]>.NotImplemented;

            [RpcReceive]
            private SerializableTypeClassFixed[] ReceiveSerializableClassArrayFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassFixedArray[version];
            }

            [RpcReceive]
            private async Task<SerializableTypeClassFixed[]> ReceiveSerializableClassArrayFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassFixedArray[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentFixed))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassFixed>> InvokeFromClientSerializableClassArraySegmentFixed(int version) => RpcTask<ArraySegment<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentFixed))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassFixed>> InvokeFromServerSerializableClassArraySegmentFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentFixedTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassFixed>> InvokeTaskFromClientSerializableClassArraySegmentFixed(int version) => RpcTask<ArraySegment<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentFixedTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassFixed>> InvokeTaskFromServerSerializableClassArraySegmentFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeClassFixed>>.NotImplemented;

            [RpcReceive]
            private ArraySegment<SerializableTypeClassFixed> ReceiveSerializableClassArraySegmentFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassFixedArraySegment[version];
            }

            [RpcReceive]
            private async Task<ArraySegment<SerializableTypeClassFixed>> ReceiveSerializableClassArraySegmentFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassFixedArraySegment[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassICollectionFixed))]
            public virtual RpcTask<ICollection<SerializableTypeClassFixed>> InvokeFromClientSerializableClassICollectionFixed(int version) => RpcTask<ICollection<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassICollectionFixed))]
            public virtual RpcTask<ICollection<SerializableTypeClassFixed>> InvokeFromServerSerializableClassICollectionFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<ICollection<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassICollectionFixedTask))]
            public virtual RpcTask<ICollection<SerializableTypeClassFixed>> InvokeTaskFromClientSerializableClassICollectionFixed(int version) => RpcTask<ICollection<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassICollectionFixedTask))]
            public virtual RpcTask<ICollection<SerializableTypeClassFixed>> InvokeTaskFromServerSerializableClassICollectionFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<ICollection<SerializableTypeClassFixed>>.NotImplemented;

            [RpcReceive]
            private ICollection<SerializableTypeClassFixed> ReceiveSerializableClassICollectionFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassFixedICollection[version];
            }

            [RpcReceive]
            private async Task<ICollection<SerializableTypeClassFixed>> ReceiveSerializableClassICollectionFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassFixedICollection[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionFixed))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeClassFixed>> InvokeFromClientSerializableClassIReadOnlyCollectionFixed(int version) => RpcTask<IReadOnlyCollection<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionFixed))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeClassFixed>> InvokeFromServerSerializableClassIReadOnlyCollectionFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<IReadOnlyCollection<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionFixedTask))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeClassFixed>> InvokeTaskFromClientSerializableClassIReadOnlyCollectionFixed(int version) => RpcTask<IReadOnlyCollection<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionFixedTask))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeClassFixed>> InvokeTaskFromServerSerializableClassIReadOnlyCollectionFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<IReadOnlyCollection<SerializableTypeClassFixed>>.NotImplemented;

            [RpcReceive]
            private IReadOnlyCollection<SerializableTypeClassFixed> ReceiveSerializableClassIReadOnlyCollectionFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassFixedIReadOnlyCollection[version];
            }

            [RpcReceive]
            private async Task<IReadOnlyCollection<SerializableTypeClassFixed>> ReceiveSerializableClassIReadOnlyCollectionFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassFixedIReadOnlyCollection[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableFixed))]
            public virtual RpcTask<IEnumerable<SerializableTypeClassFixed>> InvokeFromClientSerializableClassIEnumerableFixed(int version) => RpcTask<IEnumerable<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableFixed))]
            public virtual RpcTask<IEnumerable<SerializableTypeClassFixed>> InvokeFromServerSerializableClassIEnumerableFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<IEnumerable<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableFixedTask))]
            public virtual RpcTask<IEnumerable<SerializableTypeClassFixed>> InvokeTaskFromClientSerializableClassIEnumerableFixed(int version) => RpcTask<IEnumerable<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableFixedTask))]
            public virtual RpcTask<IEnumerable<SerializableTypeClassFixed>> InvokeTaskFromServerSerializableClassIEnumerableFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<IEnumerable<SerializableTypeClassFixed>>.NotImplemented;

            [RpcReceive]
            private IEnumerable<SerializableTypeClassFixed> ReceiveSerializableClassIEnumerableFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassFixedIEnumerable[version];
            }

            [RpcReceive]
            private async Task<IEnumerable<SerializableTypeClassFixed>> ReceiveSerializableClassIEnumerableFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassFixedIEnumerable[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassListFixed))]
            public virtual RpcTask<List<SerializableTypeClassFixed>> InvokeFromClientSerializableClassListFixed(int version) => RpcTask<List<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassListFixed))]
            public virtual RpcTask<List<SerializableTypeClassFixed>> InvokeFromServerSerializableClassListFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<List<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassListFixedTask))]
            public virtual RpcTask<List<SerializableTypeClassFixed>> InvokeTaskFromClientSerializableClassListFixed(int version) => RpcTask<List<SerializableTypeClassFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassListFixedTask))]
            public virtual RpcTask<List<SerializableTypeClassFixed>> InvokeTaskFromServerSerializableClassListFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<List<SerializableTypeClassFixed>>.NotImplemented;

            [RpcReceive]
            private List<SerializableTypeClassFixed> ReceiveSerializableClassListFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassFixedList[version];
            }

            [RpcReceive]
            private async Task<List<SerializableTypeClassFixed>> ReceiveSerializableClassListFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassFixedList[version];
            }


            [RpcSend(nameof(ReceiveSerializableClassArrayVariable))]
            public virtual RpcTask<SerializableTypeClassVariable[]> InvokeFromClientSerializableClassArrayVariable(int version) => RpcTask<SerializableTypeClassVariable[]>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArrayVariable))]
            public virtual RpcTask<SerializableTypeClassVariable[]> InvokeFromServerSerializableClassArrayVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeClassVariable[]>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArrayVariableTask))]
            public virtual RpcTask<SerializableTypeClassVariable[]> InvokeTaskFromClientSerializableClassArrayVariable(int version) => RpcTask<SerializableTypeClassVariable[]>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArrayVariableTask))]
            public virtual RpcTask<SerializableTypeClassVariable[]> InvokeTaskFromServerSerializableClassArrayVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeClassVariable[]>.NotImplemented;

            [RpcReceive]
            private SerializableTypeClassVariable[] ReceiveSerializableClassArrayVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassVariableArray[version];
            }

            [RpcReceive]
            private async Task<SerializableTypeClassVariable[]> ReceiveSerializableClassArrayVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassVariableArray[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentVariable))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassVariable>> InvokeFromClientSerializableClassArraySegmentVariable(int version) => RpcTask<ArraySegment<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentVariable))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassVariable>> InvokeFromServerSerializableClassArraySegmentVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentVariableTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassVariable>> InvokeTaskFromClientSerializableClassArraySegmentVariable(int version) => RpcTask<ArraySegment<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentVariableTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassVariable>> InvokeTaskFromServerSerializableClassArraySegmentVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeClassVariable>>.NotImplemented;

            [RpcReceive]
            private ArraySegment<SerializableTypeClassVariable> ReceiveSerializableClassArraySegmentVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassVariableArraySegment[version];
            }

            [RpcReceive]
            private async Task<ArraySegment<SerializableTypeClassVariable>> ReceiveSerializableClassArraySegmentVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassVariableArraySegment[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassICollectionVariable))]
            public virtual RpcTask<ICollection<SerializableTypeClassVariable>> InvokeFromClientSerializableClassICollectionVariable(int version) => RpcTask<ICollection<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassICollectionVariable))]
            public virtual RpcTask<ICollection<SerializableTypeClassVariable>> InvokeFromServerSerializableClassICollectionVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<ICollection<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassICollectionVariableTask))]
            public virtual RpcTask<ICollection<SerializableTypeClassVariable>> InvokeTaskFromClientSerializableClassICollectionVariable(int version) => RpcTask<ICollection<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassICollectionVariableTask))]
            public virtual RpcTask<ICollection<SerializableTypeClassVariable>> InvokeTaskFromServerSerializableClassICollectionVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<ICollection<SerializableTypeClassVariable>>.NotImplemented;

            [RpcReceive]
            private ICollection<SerializableTypeClassVariable> ReceiveSerializableClassICollectionVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassVariableICollection[version];
            }

            [RpcReceive]
            private async Task<ICollection<SerializableTypeClassVariable>> ReceiveSerializableClassICollectionVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassVariableICollection[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionVariable))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeClassVariable>> InvokeFromClientSerializableClassIReadOnlyCollectionVariable(int version) => RpcTask<IReadOnlyCollection<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionVariable))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeClassVariable>> InvokeFromServerSerializableClassIReadOnlyCollectionVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<IReadOnlyCollection<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionVariableTask))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeClassVariable>> InvokeTaskFromClientSerializableClassIReadOnlyCollectionVariable(int version) => RpcTask<IReadOnlyCollection<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassIReadOnlyCollectionVariableTask))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeClassVariable>> InvokeTaskFromServerSerializableClassIReadOnlyCollectionVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<IReadOnlyCollection<SerializableTypeClassVariable>>.NotImplemented;

            [RpcReceive]
            private IReadOnlyCollection<SerializableTypeClassVariable> ReceiveSerializableClassIReadOnlyCollectionVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassVariableIReadOnlyCollection[version];
            }

            [RpcReceive]
            private async Task<IReadOnlyCollection<SerializableTypeClassVariable>> ReceiveSerializableClassIReadOnlyCollectionVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassVariableIReadOnlyCollection[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableVariable))]
            public virtual RpcTask<IEnumerable<SerializableTypeClassVariable>> InvokeFromClientSerializableClassIEnumerableVariable(int version) => RpcTask<IEnumerable<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableVariable))]
            public virtual RpcTask<IEnumerable<SerializableTypeClassVariable>> InvokeFromServerSerializableClassIEnumerableVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<IEnumerable<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableVariableTask))]
            public virtual RpcTask<IEnumerable<SerializableTypeClassVariable>> InvokeTaskFromClientSerializableClassIEnumerableVariable(int version) => RpcTask<IEnumerable<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassIEnumerableVariableTask))]
            public virtual RpcTask<IEnumerable<SerializableTypeClassVariable>> InvokeTaskFromServerSerializableClassIEnumerableVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<IEnumerable<SerializableTypeClassVariable>>.NotImplemented;

            [RpcReceive]
            private IEnumerable<SerializableTypeClassVariable> ReceiveSerializableClassIEnumerableVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassVariableIEnumerable[version];
            }

            [RpcReceive]
            private async Task<IEnumerable<SerializableTypeClassVariable>> ReceiveSerializableClassIEnumerableVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassVariableIEnumerable[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassListVariable))]
            public virtual RpcTask<List<SerializableTypeClassVariable>> InvokeFromClientSerializableClassListVariable(int version) => RpcTask<List<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassListVariable))]
            public virtual RpcTask<List<SerializableTypeClassVariable>> InvokeFromServerSerializableClassListVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<List<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassListVariableTask))]
            public virtual RpcTask<List<SerializableTypeClassVariable>> InvokeTaskFromClientSerializableClassListVariable(int version) => RpcTask<List<SerializableTypeClassVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassListVariableTask))]
            public virtual RpcTask<List<SerializableTypeClassVariable>> InvokeTaskFromServerSerializableClassListVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<List<SerializableTypeClassVariable>>.NotImplemented;

            [RpcReceive]
            private List<SerializableTypeClassVariable> ReceiveSerializableClassListVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassVariableList[version];
            }

            [RpcReceive]
            private async Task<List<SerializableTypeClassVariable>> ReceiveSerializableClassListVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassVariableList[version];
            }
            [RpcSend(nameof(ReceiveSerializableStructArrayFixed))]
            public virtual RpcTask<SerializableTypeStructFixed[]> InvokeFromClientSerializableStructArrayFixed(int version) => RpcTask<SerializableTypeStructFixed[]>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArrayFixed))]
            public virtual RpcTask<SerializableTypeStructFixed[]> InvokeFromServerSerializableStructArrayFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeStructFixed[]>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArrayFixedTask))]
            public virtual RpcTask<SerializableTypeStructFixed[]> InvokeTaskFromClientSerializableStructArrayFixed(int version) => RpcTask<SerializableTypeStructFixed[]>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArrayFixedTask))]
            public virtual RpcTask<SerializableTypeStructFixed[]> InvokeTaskFromServerSerializableStructArrayFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeStructFixed[]>.NotImplemented;

            [RpcReceive]
            private SerializableTypeStructFixed[] ReceiveSerializableStructArrayFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructFixedArray[version];
            }

            [RpcReceive]
            private async Task<SerializableTypeStructFixed[]> ReceiveSerializableStructArrayFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructFixedArray[version];
            }

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentFixed))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructFixed>> InvokeFromClientSerializableStructArraySegmentFixed(int version) => RpcTask<ArraySegment<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentFixed))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructFixed>> InvokeFromServerSerializableStructArraySegmentFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentFixedTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructFixed>> InvokeTaskFromClientSerializableStructArraySegmentFixed(int version) => RpcTask<ArraySegment<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentFixedTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructFixed>> InvokeTaskFromServerSerializableStructArraySegmentFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeStructFixed>>.NotImplemented;

            [RpcReceive]
            private ArraySegment<SerializableTypeStructFixed> ReceiveSerializableStructArraySegmentFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructFixedArraySegment[version];
            }

            [RpcReceive]
            private async Task<ArraySegment<SerializableTypeStructFixed>> ReceiveSerializableStructArraySegmentFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructFixedArraySegment[version];
            }

            [RpcSend(nameof(ReceiveSerializableStructICollectionFixed))]
            public virtual RpcTask<ICollection<SerializableTypeStructFixed>> InvokeFromClientSerializableStructICollectionFixed(int version) => RpcTask<ICollection<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructICollectionFixed))]
            public virtual RpcTask<ICollection<SerializableTypeStructFixed>> InvokeFromServerSerializableStructICollectionFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<ICollection<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructICollectionFixedTask))]
            public virtual RpcTask<ICollection<SerializableTypeStructFixed>> InvokeTaskFromClientSerializableStructICollectionFixed(int version) => RpcTask<ICollection<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructICollectionFixedTask))]
            public virtual RpcTask<ICollection<SerializableTypeStructFixed>> InvokeTaskFromServerSerializableStructICollectionFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<ICollection<SerializableTypeStructFixed>>.NotImplemented;

            [RpcReceive]
            private ICollection<SerializableTypeStructFixed> ReceiveSerializableStructICollectionFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructFixedICollection[version];
            }

            [RpcReceive]
            private async Task<ICollection<SerializableTypeStructFixed>> ReceiveSerializableStructICollectionFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructFixedICollection[version];
            }

            [RpcSend(nameof(ReceiveSerializableStructIReadOnlyCollectionFixed))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeStructFixed>> InvokeFromClientSerializableStructIReadOnlyCollectionFixed(int version) => RpcTask<IReadOnlyCollection<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructIReadOnlyCollectionFixed))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeStructFixed>> InvokeFromServerSerializableStructIReadOnlyCollectionFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<IReadOnlyCollection<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructIReadOnlyCollectionFixedTask))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeStructFixed>> InvokeTaskFromClientSerializableStructIReadOnlyCollectionFixed(int version) => RpcTask<IReadOnlyCollection<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructIReadOnlyCollectionFixedTask))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeStructFixed>> InvokeTaskFromServerSerializableStructIReadOnlyCollectionFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<IReadOnlyCollection<SerializableTypeStructFixed>>.NotImplemented;

            [RpcReceive]
            private IReadOnlyCollection<SerializableTypeStructFixed> ReceiveSerializableStructIReadOnlyCollectionFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructFixedIReadOnlyCollection[version];
            }

            [RpcReceive]
            private async Task<IReadOnlyCollection<SerializableTypeStructFixed>> ReceiveSerializableStructIReadOnlyCollectionFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructFixedIReadOnlyCollection[version];
            }

            [RpcSend(nameof(ReceiveSerializableStructIEnumerableFixed))]
            public virtual RpcTask<IEnumerable<SerializableTypeStructFixed>> InvokeFromClientSerializableStructIEnumerableFixed(int version) => RpcTask<IEnumerable<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructIEnumerableFixed))]
            public virtual RpcTask<IEnumerable<SerializableTypeStructFixed>> InvokeFromServerSerializableStructIEnumerableFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<IEnumerable<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructIEnumerableFixedTask))]
            public virtual RpcTask<IEnumerable<SerializableTypeStructFixed>> InvokeTaskFromClientSerializableStructIEnumerableFixed(int version) => RpcTask<IEnumerable<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructIEnumerableFixedTask))]
            public virtual RpcTask<IEnumerable<SerializableTypeStructFixed>> InvokeTaskFromServerSerializableStructIEnumerableFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<IEnumerable<SerializableTypeStructFixed>>.NotImplemented;

            [RpcReceive]
            private IEnumerable<SerializableTypeStructFixed> ReceiveSerializableStructIEnumerableFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructFixedIEnumerable[version];
            }

            [RpcReceive]
            private async Task<IEnumerable<SerializableTypeStructFixed>> ReceiveSerializableStructIEnumerableFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructFixedIEnumerable[version];
            }

            [RpcSend(nameof(ReceiveSerializableStructListFixed))]
            public virtual RpcTask<List<SerializableTypeStructFixed>> InvokeFromClientSerializableStructListFixed(int version) => RpcTask<List<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructListFixed))]
            public virtual RpcTask<List<SerializableTypeStructFixed>> InvokeFromServerSerializableStructListFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<List<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructListFixedTask))]
            public virtual RpcTask<List<SerializableTypeStructFixed>> InvokeTaskFromClientSerializableStructListFixed(int version) => RpcTask<List<SerializableTypeStructFixed>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructListFixedTask))]
            public virtual RpcTask<List<SerializableTypeStructFixed>> InvokeTaskFromServerSerializableStructListFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<List<SerializableTypeStructFixed>>.NotImplemented;

            [RpcReceive]
            private List<SerializableTypeStructFixed> ReceiveSerializableStructListFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructFixedList[version];
            }

            [RpcReceive]
            private async Task<List<SerializableTypeStructFixed>> ReceiveSerializableStructListFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructFixedList[version];
            }


            [RpcSend(nameof(ReceiveSerializableStructArrayVariable))]
            public virtual RpcTask<SerializableTypeStructVariable[]> InvokeFromClientSerializableStructArrayVariable(int version) => RpcTask<SerializableTypeStructVariable[]>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArrayVariable))]
            public virtual RpcTask<SerializableTypeStructVariable[]> InvokeFromServerSerializableStructArrayVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeStructVariable[]>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArrayVariableTask))]
            public virtual RpcTask<SerializableTypeStructVariable[]> InvokeTaskFromClientSerializableStructArrayVariable(int version) => RpcTask<SerializableTypeStructVariable[]>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArrayVariableTask))]
            public virtual RpcTask<SerializableTypeStructVariable[]> InvokeTaskFromServerSerializableStructArrayVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<SerializableTypeStructVariable[]>.NotImplemented;

            [RpcReceive]
            private SerializableTypeStructVariable[] ReceiveSerializableStructArrayVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructVariableArray[version];
            }

            [RpcReceive]
            private async Task<SerializableTypeStructVariable[]> ReceiveSerializableStructArrayVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructVariableArray[version];
            }

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentVariable))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructVariable>> InvokeFromClientSerializableStructArraySegmentVariable(int version) => RpcTask<ArraySegment<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentVariable))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructVariable>> InvokeFromServerSerializableStructArraySegmentVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentVariableTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructVariable>> InvokeTaskFromClientSerializableStructArraySegmentVariable(int version) => RpcTask<ArraySegment<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentVariableTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructVariable>> InvokeTaskFromServerSerializableStructArraySegmentVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeStructVariable>>.NotImplemented;

            [RpcReceive]
            private ArraySegment<SerializableTypeStructVariable> ReceiveSerializableStructArraySegmentVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructVariableArraySegment[version];
            }

            [RpcReceive]
            private async Task<ArraySegment<SerializableTypeStructVariable>> ReceiveSerializableStructArraySegmentVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructVariableArraySegment[version];
            }

            [RpcSend(nameof(ReceiveSerializableStructICollectionVariable))]
            public virtual RpcTask<ICollection<SerializableTypeStructVariable>> InvokeFromClientSerializableStructICollectionVariable(int version) => RpcTask<ICollection<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructICollectionVariable))]
            public virtual RpcTask<ICollection<SerializableTypeStructVariable>> InvokeFromServerSerializableStructICollectionVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<ICollection<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructICollectionVariableTask))]
            public virtual RpcTask<ICollection<SerializableTypeStructVariable>> InvokeTaskFromClientSerializableStructICollectionVariable(int version) => RpcTask<ICollection<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructICollectionVariableTask))]
            public virtual RpcTask<ICollection<SerializableTypeStructVariable>> InvokeTaskFromServerSerializableStructICollectionVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<ICollection<SerializableTypeStructVariable>>.NotImplemented;

            [RpcReceive]
            private ICollection<SerializableTypeStructVariable> ReceiveSerializableStructICollectionVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructVariableICollection[version];
            }

            [RpcReceive]
            private async Task<ICollection<SerializableTypeStructVariable>> ReceiveSerializableStructICollectionVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructVariableICollection[version];
            }

            [RpcSend(nameof(ReceiveSerializableStructIReadOnlyCollectionVariable))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeStructVariable>> InvokeFromClientSerializableStructIReadOnlyCollectionVariable(int version) => RpcTask<IReadOnlyCollection<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructIReadOnlyCollectionVariable))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeStructVariable>> InvokeFromServerSerializableStructIReadOnlyCollectionVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<IReadOnlyCollection<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructIReadOnlyCollectionVariableTask))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeStructVariable>> InvokeTaskFromClientSerializableStructIReadOnlyCollectionVariable(int version) => RpcTask<IReadOnlyCollection<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructIReadOnlyCollectionVariableTask))]
            public virtual RpcTask<IReadOnlyCollection<SerializableTypeStructVariable>> InvokeTaskFromServerSerializableStructIReadOnlyCollectionVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<IReadOnlyCollection<SerializableTypeStructVariable>>.NotImplemented;

            [RpcReceive]
            private IReadOnlyCollection<SerializableTypeStructVariable> ReceiveSerializableStructIReadOnlyCollectionVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructVariableIReadOnlyCollection[version];
            }

            [RpcReceive]
            private async Task<IReadOnlyCollection<SerializableTypeStructVariable>> ReceiveSerializableStructIReadOnlyCollectionVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructVariableIReadOnlyCollection[version];
            }

            [RpcSend(nameof(ReceiveSerializableStructIEnumerableVariable))]
            public virtual RpcTask<IEnumerable<SerializableTypeStructVariable>> InvokeFromClientSerializableStructIEnumerableVariable(int version) => RpcTask<IEnumerable<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructIEnumerableVariable))]
            public virtual RpcTask<IEnumerable<SerializableTypeStructVariable>> InvokeFromServerSerializableStructIEnumerableVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<IEnumerable<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructIEnumerableVariableTask))]
            public virtual RpcTask<IEnumerable<SerializableTypeStructVariable>> InvokeTaskFromClientSerializableStructIEnumerableVariable(int version) => RpcTask<IEnumerable<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructIEnumerableVariableTask))]
            public virtual RpcTask<IEnumerable<SerializableTypeStructVariable>> InvokeTaskFromServerSerializableStructIEnumerableVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<IEnumerable<SerializableTypeStructVariable>>.NotImplemented;

            [RpcReceive]
            private IEnumerable<SerializableTypeStructVariable> ReceiveSerializableStructIEnumerableVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructVariableIEnumerable[version];
            }

            [RpcReceive]
            private async Task<IEnumerable<SerializableTypeStructVariable>> ReceiveSerializableStructIEnumerableVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructVariableIEnumerable[version];
            }

            [RpcSend(nameof(ReceiveSerializableStructListVariable))]
            public virtual RpcTask<List<SerializableTypeStructVariable>> InvokeFromClientSerializableStructListVariable(int version) => RpcTask<List<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructListVariable))]
            public virtual RpcTask<List<SerializableTypeStructVariable>> InvokeFromServerSerializableStructListVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<List<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructListVariableTask))]
            public virtual RpcTask<List<SerializableTypeStructVariable>> InvokeTaskFromClientSerializableStructListVariable(int version) => RpcTask<List<SerializableTypeStructVariable>>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructListVariableTask))]
            public virtual RpcTask<List<SerializableTypeStructVariable>> InvokeTaskFromServerSerializableStructListVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<List<SerializableTypeStructVariable>>.NotImplemented;

            [RpcReceive]
            private List<SerializableTypeStructVariable> ReceiveSerializableStructListVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructVariableList[version];
            }

            [RpcReceive]
            private async Task<List<SerializableTypeStructVariable>> ReceiveSerializableStructListVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructVariableList[version];
            }
        }
    }

    public class CollectionImpl<T> : ICollection<T>
    {
        public List<T> List;

        public CollectionImpl() : this(new List<T>()) { }
        public CollectionImpl(List<T> list)
        {
            List = list;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)List).GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            List.Add(item);
        }

        /// <inheritdoc />
        public void Clear()
        {
            List.Clear();
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return List.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            List.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            return List.Remove(item);
        }

        /// <inheritdoc />
        public int Count => List.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;
    }

    public class ReadOnlyCollectionImpl<T> : IReadOnlyCollection<T>
    {
        public List<T> List;

        public ReadOnlyCollectionImpl(List<T> list)
        {
            List = list;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)List).GetEnumerator();
        }

        /// <inheritdoc />
        public int Count => List.Count;
    }
    public class EnumerableImpl<T> : IEnumerable<T>
    {
        public List<T> List;

        public EnumerableImpl(List<T> list)
        {
            List = list;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)List).GetEnumerator();
        }
    }
}