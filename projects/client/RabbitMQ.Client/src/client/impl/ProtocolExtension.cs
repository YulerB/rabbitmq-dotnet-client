using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Framing
{
    public partial class Protocol
    {
        public RabbitMQ.Client.Impl.MethodBase DecodeMethodFrom2(RabbitMQ.Util.NetworkArraySegmentsReader reader)
        {
            ushort classId = reader.ReadUInt16();
            ushort methodId = reader.ReadUInt16();

            switch (classId)
            {
                case 10:
                    {
                        switch (methodId)
                        {
                            case 10:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConnectionStart result = new RabbitMQ.Client.Framing.Impl.ConnectionStart();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 11:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConnectionStartOk result = new RabbitMQ.Client.Framing.Impl.ConnectionStartOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 20:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConnectionSecure result = new RabbitMQ.Client.Framing.Impl.ConnectionSecure();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 21:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConnectionSecureOk result = new RabbitMQ.Client.Framing.Impl.ConnectionSecureOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 30:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConnectionTune result = new RabbitMQ.Client.Framing.Impl.ConnectionTune();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 31:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConnectionTuneOk result = new RabbitMQ.Client.Framing.Impl.ConnectionTuneOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 40:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConnectionOpen result = new RabbitMQ.Client.Framing.Impl.ConnectionOpen();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 41:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConnectionOpenOk result = new RabbitMQ.Client.Framing.Impl.ConnectionOpenOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 50:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConnectionClose result = new RabbitMQ.Client.Framing.Impl.ConnectionClose();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 51:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConnectionCloseOk result = new RabbitMQ.Client.Framing.Impl.ConnectionCloseOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 60:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConnectionBlocked result = new RabbitMQ.Client.Framing.Impl.ConnectionBlocked();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 61:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConnectionUnblocked result = new RabbitMQ.Client.Framing.Impl.ConnectionUnblocked();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            default: break;
                        }
                        break;
                    }
                case 20:
                    {
                        switch (methodId)
                        {
                            case 10:
                                {
                                    RabbitMQ.Client.Framing.Impl.ChannelOpen result = new RabbitMQ.Client.Framing.Impl.ChannelOpen();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 11:
                                {
                                    RabbitMQ.Client.Framing.Impl.ChannelOpenOk result = new RabbitMQ.Client.Framing.Impl.ChannelOpenOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 20:
                                {
                                    RabbitMQ.Client.Framing.Impl.ChannelFlow result = new RabbitMQ.Client.Framing.Impl.ChannelFlow();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 21:
                                {
                                    RabbitMQ.Client.Framing.Impl.ChannelFlowOk result = new RabbitMQ.Client.Framing.Impl.ChannelFlowOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 40:
                                {
                                    RabbitMQ.Client.Framing.Impl.ChannelClose result = new RabbitMQ.Client.Framing.Impl.ChannelClose();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 41:
                                {
                                    RabbitMQ.Client.Framing.Impl.ChannelCloseOk result = new RabbitMQ.Client.Framing.Impl.ChannelCloseOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            default: break;
                        }
                        break;
                    }
                case 40:
                    {
                        switch (methodId)
                        {
                            case 10:
                                {
                                    RabbitMQ.Client.Framing.Impl.ExchangeDeclare result = new RabbitMQ.Client.Framing.Impl.ExchangeDeclare();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 11:
                                {
                                    RabbitMQ.Client.Framing.Impl.ExchangeDeclareOk result = new RabbitMQ.Client.Framing.Impl.ExchangeDeclareOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 20:
                                {
                                    RabbitMQ.Client.Framing.Impl.ExchangeDelete result = new RabbitMQ.Client.Framing.Impl.ExchangeDelete();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 21:
                                {
                                    RabbitMQ.Client.Framing.Impl.ExchangeDeleteOk result = new RabbitMQ.Client.Framing.Impl.ExchangeDeleteOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 30:
                                {
                                    RabbitMQ.Client.Framing.Impl.ExchangeBind result = new RabbitMQ.Client.Framing.Impl.ExchangeBind();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 31:
                                {
                                    RabbitMQ.Client.Framing.Impl.ExchangeBindOk result = new RabbitMQ.Client.Framing.Impl.ExchangeBindOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 40:
                                {
                                    RabbitMQ.Client.Framing.Impl.ExchangeUnbind result = new RabbitMQ.Client.Framing.Impl.ExchangeUnbind();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 51:
                                {
                                    RabbitMQ.Client.Framing.Impl.ExchangeUnbindOk result = new RabbitMQ.Client.Framing.Impl.ExchangeUnbindOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            default: break;
                        }
                        break;
                    }
                case 50:
                    {
                        switch (methodId)
                        {
                            case 10:
                                {
                                    RabbitMQ.Client.Framing.Impl.QueueDeclare result = new RabbitMQ.Client.Framing.Impl.QueueDeclare();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 11:
                                {
                                    RabbitMQ.Client.Framing.Impl.QueueDeclareOk result = new RabbitMQ.Client.Framing.Impl.QueueDeclareOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 20:
                                {
                                    RabbitMQ.Client.Framing.Impl.QueueBind result = new RabbitMQ.Client.Framing.Impl.QueueBind();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 21:
                                {
                                    RabbitMQ.Client.Framing.Impl.QueueBindOk result = new RabbitMQ.Client.Framing.Impl.QueueBindOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 50:
                                {
                                    RabbitMQ.Client.Framing.Impl.QueueUnbind result = new RabbitMQ.Client.Framing.Impl.QueueUnbind();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 51:
                                {
                                    RabbitMQ.Client.Framing.Impl.QueueUnbindOk result = new RabbitMQ.Client.Framing.Impl.QueueUnbindOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 30:
                                {
                                    RabbitMQ.Client.Framing.Impl.QueuePurge result = new RabbitMQ.Client.Framing.Impl.QueuePurge();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 31:
                                {
                                    RabbitMQ.Client.Framing.Impl.QueuePurgeOk result = new RabbitMQ.Client.Framing.Impl.QueuePurgeOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 40:
                                {
                                    RabbitMQ.Client.Framing.Impl.QueueDelete result = new RabbitMQ.Client.Framing.Impl.QueueDelete();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 41:
                                {
                                    RabbitMQ.Client.Framing.Impl.QueueDeleteOk result = new RabbitMQ.Client.Framing.Impl.QueueDeleteOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            default: break;
                        }
                        break;
                    }
                case 60:
                    {
                        switch (methodId)
                        {
                            case 10:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicQos result = new RabbitMQ.Client.Framing.Impl.BasicQos();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 11:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicQosOk result = new RabbitMQ.Client.Framing.Impl.BasicQosOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 20:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicConsume result = new RabbitMQ.Client.Framing.Impl.BasicConsume();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 21:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicConsumeOk result = new RabbitMQ.Client.Framing.Impl.BasicConsumeOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 30:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicCancel result = new RabbitMQ.Client.Framing.Impl.BasicCancel();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 31:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicCancelOk result = new RabbitMQ.Client.Framing.Impl.BasicCancelOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 40:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicPublish result = new RabbitMQ.Client.Framing.Impl.BasicPublish();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 50:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicReturn result = new RabbitMQ.Client.Framing.Impl.BasicReturn();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 60:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicDeliver result = new RabbitMQ.Client.Framing.Impl.BasicDeliver();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 70:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicGet result = new RabbitMQ.Client.Framing.Impl.BasicGet();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 71:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicGetOk result = new RabbitMQ.Client.Framing.Impl.BasicGetOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 72:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicGetEmpty result = new RabbitMQ.Client.Framing.Impl.BasicGetEmpty();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 80:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicAck result = new RabbitMQ.Client.Framing.Impl.BasicAck();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 90:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicReject result = new RabbitMQ.Client.Framing.Impl.BasicReject();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 100:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicRecoverAsync result = new RabbitMQ.Client.Framing.Impl.BasicRecoverAsync();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 110:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicRecover result = new RabbitMQ.Client.Framing.Impl.BasicRecover();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 111:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicRecoverOk result = new RabbitMQ.Client.Framing.Impl.BasicRecoverOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 120:
                                {
                                    RabbitMQ.Client.Framing.Impl.BasicNack result = new RabbitMQ.Client.Framing.Impl.BasicNack();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            default: break;
                        }
                        break;
                    }
                case 90:
                    {
                        switch (methodId)
                        {
                            case 10:
                                {
                                    RabbitMQ.Client.Framing.Impl.TxSelect result = new RabbitMQ.Client.Framing.Impl.TxSelect();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 11:
                                {
                                    RabbitMQ.Client.Framing.Impl.TxSelectOk result = new RabbitMQ.Client.Framing.Impl.TxSelectOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 20:
                                {
                                    RabbitMQ.Client.Framing.Impl.TxCommit result = new RabbitMQ.Client.Framing.Impl.TxCommit();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 21:
                                {
                                    RabbitMQ.Client.Framing.Impl.TxCommitOk result = new RabbitMQ.Client.Framing.Impl.TxCommitOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 30:
                                {
                                    RabbitMQ.Client.Framing.Impl.TxRollback result = new RabbitMQ.Client.Framing.Impl.TxRollback();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 31:
                                {
                                    RabbitMQ.Client.Framing.Impl.TxRollbackOk result = new RabbitMQ.Client.Framing.Impl.TxRollbackOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            default: break;
                        }
                        break;
                    }
                case 85:
                    {
                        switch (methodId)
                        {
                            case 10:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConfirmSelect result = new RabbitMQ.Client.Framing.Impl.ConfirmSelect();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            case 11:
                                {
                                    RabbitMQ.Client.Framing.Impl.ConfirmSelectOk result = new RabbitMQ.Client.Framing.Impl.ConfirmSelectOk();
                                    result.ReadArgumentsFrom(new RabbitMQ.Client.Impl.MethodArgumentReader2(reader));
                                    return result;
                                }
                            default: break;
                        }
                        break;
                    }
                default: break;
            }
            throw new RabbitMQ.Client.Impl.UnknownClassOrMethodException(classId, methodId);
        }
        public RabbitMQ.Client.Impl.ContentHeaderBase DecodeContentHeaderFrom2(RabbitMQ.Util.NetworkArraySegmentsReader reader)
        {
            ushort classId = reader.ReadUInt16();

            switch (classId)
            {
                case 60: return new BasicProperties();
                default: break;
            }
            throw new RabbitMQ.Client.Impl.UnknownClassOrMethodException(classId, 0);
        }

    }
}
