﻿// Autogenerated code. Do not edit.

// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 1.1.
//
// The APL v2.0:
//
//---------------------------------------------------------------------------
//   Copyright (c) 2007-2016 Pivotal Software, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//---------------------------------------------------------------------------
//
// The MPL v1.1:
//
//---------------------------------------------------------------------------
//   The contents of this file are subject to the Mozilla Public License
//   Version 1.1 (the "License"); you may not use this file except in
//   compliance with the License. You may obtain a copy of the License at
//   http://www.rabbitmq.com/mpl.html
//
//   Software distributed under the License is distributed on an "AS IS"
//   basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
//   License for the specific language governing rights and limitations
//   under the License.
//
//   The Original Code is RabbitMQ.
//
//   The Initial Developer of the Original Code is Pivotal Software, Inc.
//   Copyright (c) 2007-2016 Pivotal Software, Inc.  All rights reserved.
//---------------------------------------------------------------------------

using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Client.Impl;
using RabbitMQ.Util;
using System;

namespace RabbitMQ.Client.Framing
{
    /// <summary>Autogenerated type. AMQP specification content header properties for content class "basic"</summary>
    public class BasicProperties : RabbitMQ.Client.Impl.BasicProperties
    {
        private string m_contentType;
        private string m_contentEncoding;
        private System.Collections.Generic.IDictionary<string, object> m_headers;
        private byte m_deliveryMode;
        private byte m_priority;
        private string m_correlationId;
        private string m_replyTo;
        private string m_expiration;
        private string m_messageId;
        private AmqpTimestamp m_timestamp;
        private string m_type;
        private string m_userId;
        private string m_appId;
        private string m_clusterId;

        private bool m_contentType_present = false;
        private bool m_contentEncoding_present = false;
        private bool m_headers_present = false;
        private bool m_deliveryMode_present = false;
        private bool m_priority_present = false;
        private bool m_correlationId_present = false;
        private bool m_replyTo_present = false;
        private bool m_expiration_present = false;
        private bool m_messageId_present = false;
        private bool m_timestamp_present = false;
        private bool m_type_present = false;
        private bool m_userId_present = false;
        private bool m_appId_present = false;
        private bool m_clusterId_present = false;

        public override string ContentType
        {
            get
            {
                return m_contentType;
            }
            set
            {
                m_contentType_present = value != null;
                m_contentType = value;
            }
        }
        public override string ContentEncoding
        {
            get
            {
                return m_contentEncoding;
            }
            set
            {
                m_contentEncoding_present = value != null;
                m_contentEncoding = value;
            }
        }
        public override System.Collections.Generic.IDictionary<string, object> Headers
        {
            get
            {
                return m_headers;
            }
            set
            {
                m_headers_present = value != null;
                m_headers = value;
            }
        }
        public override byte DeliveryMode
        {
            get
            {
                return m_deliveryMode;
            }
            set
            {
                m_deliveryMode_present = value != default(byte);
                m_deliveryMode = value;
            }
        }
        public override byte Priority
        {
            get
            {
                return m_priority;
            }
            set
            {
                m_priority_present = value != default(byte);
                m_priority = value;
            }
        }
        public override string CorrelationId
        {
            get
            {
                return m_correlationId;
            }
            set
            {
                m_correlationId_present = value!=null;
                m_correlationId = value;
            }
        }
        public override string ReplyTo
        {
            get
            {
                return m_replyTo;
            }
            set
            {
                m_replyTo_present = value != null;
                m_replyTo = value;
            }
        }
        public override string Expiration
        {
            get
            {
                return m_expiration;
            }
            set
            {
                m_expiration_present = value != null;
                m_expiration = value;
            }
        }
        public override string MessageId
        {
            get
            {
                return m_messageId;
            }
            set
            {
                m_messageId_present = value != null;
                m_messageId = value;
            }
        }
        public override AmqpTimestamp Timestamp
        {
            get
            {
                return m_timestamp;
            }
            set
            {
                m_timestamp_present = true;
                m_timestamp = value;
            }
        }
        public override string Type
        {
            get
            {
                return m_type;
            }
            set
            {
                m_type_present = value != null;
                m_type = value;
            }
        }
        public override string UserId
        {
            get
            {
                return m_userId;
            }
            set
            {
                m_userId_present = value != null;
                m_userId = value;
            }
        }
        public override string AppId
        {
            get
            {
                return m_appId;
            }
            set
            {
                m_appId_present = value != null;
                m_appId = value;
            }
        }
        public override string ClusterId
        {
            get
            {
                return m_clusterId;
            }
            set
            {
                m_clusterId_present = value != null;
                m_clusterId = value;
            }
        }

        public override void ClearContentType() { m_contentType_present = false; }
        public override void ClearContentEncoding() { m_contentEncoding_present = false; }
        public override void ClearHeaders() { m_headers_present = false; }
        public override void ClearDeliveryMode() { m_deliveryMode_present = false; }
        public override void ClearPriority() { m_priority_present = false; }
        public override void ClearCorrelationId() { m_correlationId_present = false; }
        public override void ClearReplyTo() { m_replyTo_present = false; }
        public override void ClearExpiration() { m_expiration_present = false; }
        public override void ClearMessageId() { m_messageId_present = false; }
        public override void ClearTimestamp() { m_timestamp_present = false; }
        public override void ClearType() { m_type_present = false; }
        public override void ClearUserId() { m_userId_present = false; }
        public override void ClearAppId() { m_appId_present = false; }
        public override void ClearClusterId() { m_clusterId_present = false; }

        public override bool IsContentTypePresent() { return m_contentType_present; }
        public override bool IsContentEncodingPresent() { return m_contentEncoding_present; }
        public override bool IsHeadersPresent() { return m_headers_present; }
        public override bool IsDeliveryModePresent() { return m_deliveryMode_present; }
        public override bool IsPriorityPresent() { return m_priority_present; }
        public override bool IsCorrelationIdPresent() { return m_correlationId_present; }
        public override bool IsReplyToPresent() { return m_replyTo_present; }
        public override bool IsExpirationPresent() { return m_expiration_present; }
        public override bool IsMessageIdPresent() { return m_messageId_present; }
        public override bool IsTimestampPresent() { return m_timestamp_present; }
        public override bool IsTypePresent() { return m_type_present; }
        public override bool IsUserIdPresent() { return m_userId_present; }
        public override bool IsAppIdPresent() { return m_appId_present; }
        public override bool IsClusterIdPresent() { return m_clusterId_present; }

        public BasicProperties() { }
        public override ushort ProtocolClassId { get { return 60; } }
        public override string ProtocolClassName { get { return "basic"; } }

        public override void ReadPropertiesFrom(ArraySegmentSequence stream)
        {
            BasicPropertiesPresense flags = (BasicPropertiesPresense)stream.ReadUInt16();

            m_contentType_present = (flags & BasicPropertiesPresense.HasContentType) == BasicPropertiesPresense.HasContentType;
            m_contentEncoding_present = (flags & BasicPropertiesPresense.HascontentEncoding) == BasicPropertiesPresense.HascontentEncoding;
            m_headers_present = (flags & BasicPropertiesPresense.Hasheaders) == BasicPropertiesPresense.Hasheaders;
            m_deliveryMode_present = (flags & BasicPropertiesPresense.HasdeliveryMode) == BasicPropertiesPresense.HasdeliveryMode;
            m_priority_present = (flags & BasicPropertiesPresense.Haspriority) == BasicPropertiesPresense.Haspriority;
            m_correlationId_present = (flags & BasicPropertiesPresense.HascorrelationId) == BasicPropertiesPresense.HascorrelationId;
            m_replyTo_present = (flags & BasicPropertiesPresense.HasreplyTo) == BasicPropertiesPresense.HasreplyTo;
            m_expiration_present = (flags & BasicPropertiesPresense.Hasexpiration) == BasicPropertiesPresense.Hasexpiration;
            m_messageId_present = (flags & BasicPropertiesPresense.HasmessageId) == BasicPropertiesPresense.HasmessageId;
            m_timestamp_present = (flags & BasicPropertiesPresense.Hastimestamp) == BasicPropertiesPresense.Hastimestamp;
            m_type_present = (flags & BasicPropertiesPresense.Hastype) == BasicPropertiesPresense.Hastype;
            m_userId_present = (flags & BasicPropertiesPresense.HasuserId) == BasicPropertiesPresense.HasuserId;
            m_appId_present = (flags & BasicPropertiesPresense.HasappId) == BasicPropertiesPresense.HasappId;
            m_clusterId_present = (flags & BasicPropertiesPresense.HasclusterId) == BasicPropertiesPresense.HasclusterId;

            if (m_contentType_present) { m_contentType = stream.ReadShortString(); }
            if (m_contentEncoding_present) { m_contentEncoding = stream.ReadShortString(); }
            if (m_headers_present) { m_headers = stream.ReadTable(); }
            if (m_deliveryMode_present) { m_deliveryMode = stream.ReadByte(); }
            if (m_priority_present) { m_priority = stream.ReadByte(); }
            if (m_correlationId_present) { m_correlationId = stream.ReadShortString(); }
            if (m_replyTo_present) { m_replyTo = stream.ReadShortString(); }
            if (m_expiration_present) { m_expiration = stream.ReadShortString(); }
            if (m_messageId_present) { m_messageId = stream.ReadShortString(); }
            if (m_timestamp_present) { m_timestamp = stream.ReadTimestamp(); }
            if (m_type_present) { m_type = stream.ReadShortString(); }
            if (m_userId_present) { m_userId = stream.ReadShortString(); }
            if (m_appId_present) { m_appId = stream.ReadShortString(); }
            if (m_clusterId_present) { m_clusterId = stream.ReadShortString(); }
        }

        public override void WritePropertiesTo(FrameBuilder writer)
        {
            writer.WriteBits(new bool[] {m_contentType_present,m_contentEncoding_present,m_headers_present,
                m_deliveryMode_present,m_priority_present,m_correlationId_present,m_replyTo_present,
            m_expiration_present,m_messageId_present,m_timestamp_present,m_type_present,m_userId_present,
            m_appId_present,m_clusterId_present });

            if (m_contentType_present) { writer.WriteShortString(m_contentType); }
            if (m_contentEncoding_present) { writer.WriteShortString(m_contentEncoding); }
            if (m_headers_present) { writer.WriteTable(m_headers); }
            if (m_deliveryMode_present) { writer.WriteByte(m_deliveryMode); }
            if (m_priority_present) { writer.WriteByte(m_priority); }
            if (m_correlationId_present) { writer.WriteShortString(m_correlationId); }
            if (m_replyTo_present) { writer.WriteShortString(m_replyTo); }
            if (m_expiration_present) { writer.WriteShortString(m_expiration); }
            if (m_messageId_present) { writer.WriteShortString(m_messageId); }
            if (m_timestamp_present) { writer.WriteTimestamp(m_timestamp); }
            if (m_type_present) { writer.WriteShortString(m_type); }
            if (m_userId_present) { writer.WriteShortString(m_userId); }
            if (m_appId_present) { writer.WriteShortString(m_appId); }
            if (m_clusterId_present) { writer.WriteShortString(m_clusterId); }
        }

        public override void WritePropertiesTo(ref Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteBits(ref writer, new bool[] {m_contentType_present,m_contentEncoding_present,m_headers_present,
                m_deliveryMode_present,m_priority_present,m_correlationId_present,m_replyTo_present,
            m_expiration_present,m_messageId_present,m_timestamp_present,m_type_present,m_userId_present,
            m_appId_present,m_clusterId_present }, out int written1);

            written = written1;

            if (m_contentType_present) {
                NetworkBinaryWriter1.WriteShortString(ref writer, m_contentType, out int written2);
                written += written2;
            }
            if (m_contentEncoding_present) {
                NetworkBinaryWriter1.WriteShortString(ref writer, m_contentEncoding, out int written2);
                written += written2;
            }
            if (m_headers_present) {
                NetworkBinaryWriter1.WriteTable(ref writer, m_headers, out int written2);
                written += written2;
            }
            if (m_deliveryMode_present) {
                NetworkBinaryWriter1.WriteByte(ref writer, m_deliveryMode, out int written2);
                written += written2;
            }
            if (m_priority_present)
            {
                NetworkBinaryWriter1.WriteByte(ref writer, m_priority, out int written2);
                written += written2;
            }
            if (m_correlationId_present) {
                NetworkBinaryWriter1.WriteShortString(ref writer, m_correlationId, out int written2);
                written += written2;
            }
            if (m_replyTo_present) {
                NetworkBinaryWriter1.WriteShortString(ref writer, m_replyTo, out int written2);
                written += written2;
            }
            if (m_expiration_present) {
                NetworkBinaryWriter1.WriteShortString(ref writer, m_expiration, out int written2);
                written += written2;
            }
            if (m_messageId_present) {
                NetworkBinaryWriter1.WriteShortString(ref writer, m_messageId, out int written2);
                written += written2;
            }
            if (m_timestamp_present) {
                NetworkBinaryWriter1.WriteTimestamp(ref writer, m_timestamp, out int written2);
                written += written2;
            }
            if (m_type_present) {
                NetworkBinaryWriter1.WriteShortString(ref writer, m_type, out int written2);
                written += written2;
            }
            if (m_userId_present) {
                NetworkBinaryWriter1.WriteShortString(ref writer, m_userId, out int written2);
                written += written2;
            }
            if (m_appId_present) {
                NetworkBinaryWriter1.WriteShortString(ref writer, m_appId, out int written2);
                written += written2;
            }
            if (m_clusterId_present) {
                NetworkBinaryWriter1.WriteShortString(ref writer, m_clusterId, out int written2);
                written += written2;
            }
        }

        public override void AppendPropertyDebugStringTo(System.Text.StringBuilder sb)
        {
            sb.Append("(");
            sb.Append("content-type="); sb.Append(m_contentType_present ? (m_contentType == null ? "(null)" : m_contentType.ToString()) : "_"); sb.Append(", ");
            sb.Append("content-encoding="); sb.Append(m_contentEncoding_present ? (m_contentEncoding == null ? "(null)" : m_contentEncoding.ToString()) : "_"); sb.Append(", ");
            sb.Append("headers="); sb.Append(m_headers_present ? (m_headers == null ? "(null)" : m_headers.ToString()) : "_"); sb.Append(", ");
            sb.Append("delivery-mode="); sb.Append(m_deliveryMode_present ? m_deliveryMode.ToString() : "_"); sb.Append(", ");
            sb.Append("priority="); sb.Append(m_priority_present ? m_priority.ToString() : "_"); sb.Append(", ");
            sb.Append("correlation-id="); sb.Append(m_correlationId_present ? (m_correlationId == null ? "(null)" : m_correlationId.ToString()) : "_"); sb.Append(", ");
            sb.Append("reply-to="); sb.Append(m_replyTo_present ? (m_replyTo == null ? "(null)" : m_replyTo.ToString()) : "_"); sb.Append(", ");
            sb.Append("expiration="); sb.Append(m_expiration_present ? (m_expiration == null ? "(null)" : m_expiration.ToString()) : "_"); sb.Append(", ");
            sb.Append("message-id="); sb.Append(m_messageId_present ? (m_messageId == null ? "(null)" : m_messageId.ToString()) : "_"); sb.Append(", ");
            sb.Append("timestamp="); sb.Append(m_timestamp_present ? m_timestamp.ToString() : "_"); sb.Append(", ");
            sb.Append("type="); sb.Append(m_type_present ? (m_type == null ? "(null)" : m_type.ToString()) : "_"); sb.Append(", ");
            sb.Append("user-id="); sb.Append(m_userId_present ? (m_userId == null ? "(null)" : m_userId.ToString()) : "_"); sb.Append(", ");
            sb.Append("app-id="); sb.Append(m_appId_present ? (m_appId == null ? "(null)" : m_appId.ToString()) : "_"); sb.Append(", ");
            sb.Append("cluster-id="); sb.Append(m_clusterId_present ? (m_clusterId == null ? "(null)" : m_clusterId.ToString()) : "_");
            sb.Append(")");
        }

        internal override int EstimatePropertiesSize()
        {
            var total = 2;

            if (m_contentType_present)
            {
                total += 1 + System.Text.Encoding.UTF8.GetByteCount(m_contentType);
            }
            if (m_contentEncoding_present)
            {
                total += 1 + System.Text.Encoding.UTF8.GetByteCount(m_contentEncoding );
            }
            if (m_headers_present)
            {
                total += NetworkBinaryWriter1.EstimateTableSize(m_headers);
            }
            if (m_deliveryMode_present)
            {
                total += 1;
            }
            if (m_priority_present)
            {
                total += 1;
            }
            if (m_correlationId_present)
            {
                total += 1 + System.Text.Encoding.UTF8.GetByteCount(m_correlationId );
            }
            if (m_replyTo_present)
            {
                total += 1 + System.Text.Encoding.UTF8.GetByteCount(m_replyTo );
            }
            if (m_expiration_present)
            {
                total += 1 + System.Text.Encoding.UTF8.GetByteCount(m_expiration );
            }
            if (m_messageId_present)
            {
                total += 1 + System.Text.Encoding.UTF8.GetByteCount(m_messageId );
            }
            if (m_timestamp_present)
            {
                total += 8;
            }
            if (m_type_present)
            {
                total += 1 + System.Text.Encoding.UTF8.GetByteCount(m_type );
            }
            if (m_userId_present)
            {
                total += 1 + System.Text.Encoding.UTF8.GetByteCount(m_userId );
            }
            if (m_appId_present)
            {
                total += 1 + System.Text.Encoding.UTF8.GetByteCount(m_appId );
            }
            if (m_clusterId_present)
            {
                total += 1 + System.Text.Encoding.UTF8.GetByteCount(m_clusterId );
            }

            return total;
        }
    }
}
