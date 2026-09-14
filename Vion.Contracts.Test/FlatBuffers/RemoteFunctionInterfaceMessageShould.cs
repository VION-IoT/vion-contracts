using System;
using Google.FlatBuffers;
using Vion.Contracts.FlatBuffers.Remote.Func;

namespace Vion.Contracts.Test.FlatBuffers
{
    [TestClass]
    public class RemoteFunctionInterfaceMessageShould
    {
        [TestMethod]
        public void VerifyReturnsTrueForValidBuffer()
        {
            var buffer = BuildValidBuffer();

            var verified = RemoteFunctionInterfaceMessage.VerifyRemoteFunctionInterfaceMessage(buffer);

            Assert.IsTrue(verified);
        }

        [TestMethod]
        public void VerifyReturnsFalseForGarbageBuffer()
        {
            var garbage = new byte[64];
            new Random(1).NextBytes(garbage);
            var buffer = new ByteBuffer(garbage);

            var verified = RemoteFunctionInterfaceMessage.VerifyRemoteFunctionInterfaceMessage(buffer);

            Assert.IsFalse(verified);
        }

        // Builds the buffer the same way dale does (Dale/Mqtt/FlatBuffer/FlatBufferPayloadFactory.cs):
        // via the generated CreateRemoteFunctionInterfaceMessage builder API, then Finish.
        private static ByteBuffer BuildValidBuffer()
        {
            var builder = new FlatBufferBuilder(64);
            var fromLogicBlockIdOffset = builder.CreateString("logic-block-a");
            var fromInterfaceIdentifierOffset = builder.CreateString("interface-a");
            var toLogicBlockIdOffset = builder.CreateString("logic-block-b");
            var toInterfaceIdentifierOffset = builder.CreateString("interface-b");
            var payloadTypeFullNameOffset = builder.CreateString("Vion.Contracts.Test.SamplePayload");
            var payloadDataVector = RemoteFunctionInterfaceMessage.CreatePayloadDataVector(builder, new byte[] { 1, 2, 3 });

            var offset = RemoteFunctionInterfaceMessage.CreateRemoteFunctionInterfaceMessage(builder,
                                                                                             fromLogicBlockIdOffset,
                                                                                             fromInterfaceIdentifierOffset,
                                                                                             toLogicBlockIdOffset,
                                                                                             toInterfaceIdentifierOffset,
                                                                                             payloadTypeFullNameOffset,
                                                                                             payloadDataVector);

            RemoteFunctionInterfaceMessage.FinishRemoteFunctionInterfaceMessageBuffer(builder, offset);

            return builder.DataBuffer;
        }
    }
}
