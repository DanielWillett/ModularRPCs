using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ModularRPCs.Test.CodeGen
{
    [NonParallelizable, TestFixture]
    public class ReturnTypeNullableCollectionSerializableCollection
    {
        private static bool _wasInvoked;

        private const int RtnValueSerializableTypeClassFixedArraySegmentLength = 11;
        private static readonly ArraySegment<SerializableTypeClassFixed>?[] RtnValueSerializableTypeClassFixedArraySegment =
            new ArraySegment<SerializableTypeClassFixed>?[]
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
                null,
                default(ArraySegment<SerializableTypeClassFixed>),
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

        private const int RtnValueSerializableTypeClassVariableArraySegmentLength = 11;
        private static readonly ArraySegment<SerializableTypeClassVariable>?[] RtnValueSerializableTypeClassVariableArraySegment =
            new ArraySegment<SerializableTypeClassVariable>?[]
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
                null,
                default(ArraySegment<SerializableTypeClassVariable>),
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

        private const int RtnValueSerializableTypeStructFixedArraySegmentLength = 7;
        private static readonly ArraySegment<SerializableTypeStructFixed>?[] RtnValueSerializableTypeStructFixedArraySegment =
            new ArraySegment<SerializableTypeStructFixed>?[]
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
                null,
                default(ArraySegment<SerializableTypeStructFixed>),
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

        private const int RtnValueSerializableTypeStructVariableArraySegmentLength = 7;
        private static readonly ArraySegment<SerializableTypeStructVariable>?[] RtnValueSerializableTypeStructVariableArraySegment =
            new ArraySegment<SerializableTypeStructVariable>?[]
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
                null,
                default(ArraySegment<SerializableTypeStructVariable>),
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


        [Test]
        public async Task ServerToClientBytesSerializableTypeClassArraySegmentFixed([Range(0, RtnValueSerializableTypeClassFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassFixed>? rtnValue = await proxy.InvokeFromServerSerializableClassArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassArraySegmentFixed([Range(0, RtnValueSerializableTypeClassFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassFixed>? rtnValue = await proxy.InvokeFromClientSerializableClassArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Value.Array, Is.Null);
            }
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

            ArraySegment<SerializableTypeClassFixed>? rtnValue = await proxy.InvokeFromServerSerializableClassArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassArraySegmentFixed([Range(0, RtnValueSerializableTypeClassFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassFixed>? rtnValue = await proxy.InvokeFromClientSerializableClassArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Value.Array, Is.Null);
            }
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

            ArraySegment<SerializableTypeClassFixed>? rtnValue = await proxy.InvokeTaskFromServerSerializableClassArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassArraySegmentFixed([Range(0, RtnValueSerializableTypeClassFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassFixed>? rtnValue = await proxy.InvokeTaskFromClientSerializableClassArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Value.Array, Is.Null);
            }
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

            ArraySegment<SerializableTypeClassFixed>? rtnValue = await proxy.InvokeTaskFromServerSerializableClassArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassArraySegmentFixed([Range(0, RtnValueSerializableTypeClassFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassFixed>? rtnValue = await proxy.InvokeTaskFromClientSerializableClassArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassFixedArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassFixedArraySegment[version]));
        }


        [Test]
        public async Task ServerToClientBytesSerializableTypeClassArraySegmentVariable([Range(0, RtnValueSerializableTypeClassVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassVariable>? rtnValue = await proxy.InvokeFromServerSerializableClassArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeClassArraySegmentVariable([Range(0, RtnValueSerializableTypeClassVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassVariable>? rtnValue = await proxy.InvokeFromClientSerializableClassArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Value.Array, Is.Null);
            }
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

            ArraySegment<SerializableTypeClassVariable>? rtnValue = await proxy.InvokeFromServerSerializableClassArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeClassArraySegmentVariable([Range(0, RtnValueSerializableTypeClassVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassVariable>? rtnValue = await proxy.InvokeFromClientSerializableClassArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Value.Array, Is.Null);
            }
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

            ArraySegment<SerializableTypeClassVariable>? rtnValue = await proxy.InvokeTaskFromServerSerializableClassArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeClassArraySegmentVariable([Range(0, RtnValueSerializableTypeClassVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassVariable>? rtnValue = await proxy.InvokeTaskFromClientSerializableClassArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Value.Array, Is.Null);
            }
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

            ArraySegment<SerializableTypeClassVariable>? rtnValue = await proxy.InvokeTaskFromServerSerializableClassArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeClassArraySegmentVariable([Range(0, RtnValueSerializableTypeClassVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeClassVariable>? rtnValue = await proxy.InvokeTaskFromClientSerializableClassArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeClassVariableArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeClassVariableArraySegment[version]));
        }


        [Test]
        public async Task ServerToClientBytesSerializableTypeStructArraySegmentFixed([Range(0, RtnValueSerializableTypeStructFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructFixed>? rtnValue = await proxy.InvokeFromServerSerializableStructArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructArraySegmentFixed([Range(0, RtnValueSerializableTypeStructFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructFixed>? rtnValue = await proxy.InvokeFromClientSerializableStructArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Value.Array, Is.Null);
            }
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

            ArraySegment<SerializableTypeStructFixed>? rtnValue = await proxy.InvokeFromServerSerializableStructArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructArraySegmentFixed([Range(0, RtnValueSerializableTypeStructFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructFixed>? rtnValue = await proxy.InvokeFromClientSerializableStructArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Value.Array, Is.Null);
            }
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

            ArraySegment<SerializableTypeStructFixed>? rtnValue = await proxy.InvokeTaskFromServerSerializableStructArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructArraySegmentFixed([Range(0, RtnValueSerializableTypeStructFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructFixed>? rtnValue = await proxy.InvokeTaskFromClientSerializableStructArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Value.Array, Is.Null);
            }
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

            ArraySegment<SerializableTypeStructFixed>? rtnValue = await proxy.InvokeTaskFromServerSerializableStructArraySegmentFixed(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructArraySegmentFixed([Range(0, RtnValueSerializableTypeStructFixedArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructFixed>? rtnValue = await proxy.InvokeTaskFromClientSerializableStructArraySegmentFixed(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructFixedArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructFixedArraySegment[version]));
        }



        [Test]
        public async Task ServerToClientBytesSerializableTypeStructArraySegmentVariable([Range(0, RtnValueSerializableTypeStructVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructVariable>? rtnValue = await proxy.InvokeFromServerSerializableStructArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerBytesSerializableTypeStructArraySegmentVariable([Range(0, RtnValueSerializableTypeStructVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructVariable>? rtnValue = await proxy.InvokeFromClientSerializableStructArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Value.Array, Is.Null);
            }
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

            ArraySegment<SerializableTypeStructVariable>? rtnValue = await proxy.InvokeFromServerSerializableStructArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerStreamSerializableTypeStructArraySegmentVariable([Range(0, RtnValueSerializableTypeStructVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructVariable>? rtnValue = await proxy.InvokeFromClientSerializableStructArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Value.Array, Is.Null);
            }
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

            ArraySegment<SerializableTypeStructVariable>? rtnValue = await proxy.InvokeTaskFromServerSerializableStructArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskBytesSerializableTypeStructArraySegmentVariable([Range(0, RtnValueSerializableTypeStructVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructVariable>? rtnValue = await proxy.InvokeTaskFromClientSerializableStructArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Value.Array, Is.Null);
            }
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

            ArraySegment<SerializableTypeStructVariable>? rtnValue = await proxy.InvokeTaskFromServerSerializableStructArraySegmentVariable(version, connection);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].HasValue, Is.False);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].HasValue);
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArraySegment[version]));
        }
    
        [Test]
        public async Task ClientToServerTaskStreamSerializableTypeStructArraySegmentVariable([Range(0, RtnValueSerializableTypeStructVariableArraySegmentLength - 1)] int version)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            ArraySegment<SerializableTypeStructVariable>? rtnValue = await proxy.InvokeTaskFromClientSerializableStructArraySegmentVariable(version);

            Assert.That(_wasInvoked, Is.True);
            if (!rtnValue.HasValue)
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Null);
            else if (rtnValue.Value.Array == null)
            {
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version], Is.Not.Null);
                Assert.That(RtnValueSerializableTypeStructVariableArraySegment[version].Value.Array, Is.Null);
            }
            else
                Assert.That(rtnValue, Is.EquivalentTo(RtnValueSerializableTypeStructVariableArraySegment[version]));
        }


        [RpcClass]
        public class TestClass
        {
            [RpcSend(nameof(ReceiveSerializableClassArraySegmentFixed))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassFixed>?> InvokeFromClientSerializableClassArraySegmentFixed(int version) => RpcTask<ArraySegment<SerializableTypeClassFixed>?>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentFixed))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassFixed>?> InvokeFromServerSerializableClassArraySegmentFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeClassFixed>?>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentFixedTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassFixed>?> InvokeTaskFromClientSerializableClassArraySegmentFixed(int version) => RpcTask<ArraySegment<SerializableTypeClassFixed>?>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentFixedTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassFixed>?> InvokeTaskFromServerSerializableClassArraySegmentFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeClassFixed>?>.NotImplemented;

            [RpcReceive]
            private ArraySegment<SerializableTypeClassFixed>? ReceiveSerializableClassArraySegmentFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassFixedArraySegment[version];
            }

            [RpcReceive]
            private async Task<ArraySegment<SerializableTypeClassFixed>?> ReceiveSerializableClassArraySegmentFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassFixedArraySegment[version];
            }

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentVariable))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassVariable>?> InvokeFromClientSerializableClassArraySegmentVariable(int version) => RpcTask<ArraySegment<SerializableTypeClassVariable>?>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentVariable))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassVariable>?> InvokeFromServerSerializableClassArraySegmentVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeClassVariable>?>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentVariableTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassVariable>?> InvokeTaskFromClientSerializableClassArraySegmentVariable(int version) => RpcTask<ArraySegment<SerializableTypeClassVariable>?>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableClassArraySegmentVariableTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeClassVariable>?> InvokeTaskFromServerSerializableClassArraySegmentVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeClassVariable>?>.NotImplemented;

            [RpcReceive]
            private ArraySegment<SerializableTypeClassVariable>? ReceiveSerializableClassArraySegmentVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeClassVariableArraySegment[version];
            }

            [RpcReceive]
            private async Task<ArraySegment<SerializableTypeClassVariable>?> ReceiveSerializableClassArraySegmentVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeClassVariableArraySegment[version];
            }

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentFixed))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructFixed>?> InvokeFromClientSerializableStructArraySegmentFixed(int version) => RpcTask<ArraySegment<SerializableTypeStructFixed>?>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentFixed))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructFixed>?> InvokeFromServerSerializableStructArraySegmentFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeStructFixed>?>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentFixedTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructFixed>?> InvokeTaskFromClientSerializableStructArraySegmentFixed(int version) => RpcTask<ArraySegment<SerializableTypeStructFixed>?>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentFixedTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructFixed>?> InvokeTaskFromServerSerializableStructArraySegmentFixed(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeStructFixed>?>.NotImplemented;

            [RpcReceive]
            private ArraySegment<SerializableTypeStructFixed>? ReceiveSerializableStructArraySegmentFixed(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructFixedArraySegment[version];
            }

            [RpcReceive]
            private async Task<ArraySegment<SerializableTypeStructFixed>?> ReceiveSerializableStructArraySegmentFixedTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructFixedArraySegment[version];
            }

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentVariable))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructVariable>?> InvokeFromClientSerializableStructArraySegmentVariable(int version) => RpcTask<ArraySegment<SerializableTypeStructVariable>?>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentVariable))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructVariable>?> InvokeFromServerSerializableStructArraySegmentVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeStructVariable>?>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentVariableTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructVariable>?> InvokeTaskFromClientSerializableStructArraySegmentVariable(int version) => RpcTask<ArraySegment<SerializableTypeStructVariable>?>.NotImplemented;

            [RpcSend(nameof(ReceiveSerializableStructArraySegmentVariableTask))]
            public virtual RpcTask<ArraySegment<SerializableTypeStructVariable>?> InvokeTaskFromServerSerializableStructArraySegmentVariable(int version, IModularRpcRemoteConnection connection) => RpcTask<ArraySegment<SerializableTypeStructVariable>?>.NotImplemented;

            [RpcReceive]
            private ArraySegment<SerializableTypeStructVariable>? ReceiveSerializableStructArraySegmentVariable(int version)
            {
                _wasInvoked = true;

                return RtnValueSerializableTypeStructVariableArraySegment[version];
            }

            [RpcReceive]
            private async Task<ArraySegment<SerializableTypeStructVariable>?> ReceiveSerializableStructArraySegmentVariableTask(int version)
            {
                _wasInvoked = true;

                await Task.Delay(1);

                return RtnValueSerializableTypeStructVariableArraySegment[version];
            }
        }
    }
}