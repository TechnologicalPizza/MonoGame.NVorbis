﻿/****************************************************************************
 * NVorbis                                                                  *
 * Copyright (C) 2014, Andrew Ward <afward@gmail.com>                       *
 *                                                                          *
 * See COPYING for license terms (Ms-PL).                                   *
 *                                                                          *
 ***************************************************************************/
using System;
using System.IO;

namespace NVorbis
{
    /// <summary>
    /// A thread-safe, read-only, buffering stream wrapper.
    /// </summary>
    partial class BufferedReadStream : Stream
    {
        Stream _baseStream;
        bool _leaveOpen;

        StreamReadBuffer _buffer;
        long _readPosition;
        object _localLock = new object();
        System.Threading.Thread _owningThread;
        int _lockCount;

        public BufferedReadStream(Stream baseStream, bool leaveOpen)
            : this(baseStream, leaveOpen, minimalRead: false)
        {
        }

        private BufferedReadStream(
            Stream baseStream, bool leaveOpen, bool minimalRead)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));

            if (!baseStream.CanRead)
                throw new ArgumentException(nameof(baseStream), "Stream is not readable.");

            _baseStream = baseStream;
            _leaveOpen = leaveOpen;
            _buffer = new StreamReadBuffer(baseStream, minimalRead);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (_buffer != null)
                {
                    _buffer.Dispose();
                    _buffer = null;
                }
                if (!_leaveOpen)
                    _baseStream.Dispose();
            }
        }

        // route all the container locking through here so we can track whether the caller actually took the lock...
        public void TakeLock()
        {
            System.Threading.Monitor.Enter(_localLock);
            if (++_lockCount == 1)
                _owningThread = System.Threading.Thread.CurrentThread;
        }

        void CheckLock()
        {
            if (_owningThread != System.Threading.Thread.CurrentThread)
                throw new System.Threading.SynchronizationLockException();
        }

        public void ReleaseLock()
        {
            CheckLock();
            if (--_lockCount == 0)
                _owningThread = null;

            System.Threading.Monitor.Exit(_localLock);
        }

        public bool MinimalRead
        {
            get => _buffer.MinimalRead;
            set => _buffer.MinimalRead = value;
        }

        public long BufferBaseOffset => _buffer.BaseOffset;
        public int BufferBytesFilled => _buffer.BytesFilled;

        public void Discard(int bytes)
        {
            CheckLock();
            _buffer.DiscardThrough(_buffer.BaseOffset + bytes);
        }

        public void DiscardThrough(long offset)
        {
            CheckLock();
            _buffer.DiscardThrough(offset);
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;

        public override void Flush()
        {
        }

        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _readPosition;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override int ReadByte()
        {
            CheckLock();
            var val = _buffer.ReadByte(Position);
            if (val > -1)
                Seek(1, SeekOrigin.Current);

            return val;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckLock();
            int cnt = _buffer.Read(Position, buffer, offset, count);
            Seek(cnt, SeekOrigin.Current);
            return cnt;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckLock();
            switch (origin)
            {
                case SeekOrigin.Begin:
                    // no-op
                    break;
                case SeekOrigin.Current:
                    offset += Position;
                    break;
                case SeekOrigin.End:
                    offset += Length;
                    break;
            }

            if (!_baseStream.CanSeek)
            {
                if (offset < _buffer.BaseOffset)
                    throw new InvalidOperationException("Cannot seek to before the start of the buffer.");
                if (offset >= _buffer.BufferEndOffset)
                    throw new InvalidOperationException("Cannot seek to beyond the end of the buffer. Discard some bytes.");
            }

            return _readPosition = offset;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
