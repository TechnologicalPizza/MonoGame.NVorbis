﻿/****************************************************************************
 * NVorbis                                                                  *
 * Copyright (C) 2014, Andrew Ward <afward@gmail.com>                       *
 *                                                                          *
 * See COPYING for license terms (Ms-PL).                                   *
 *                                                                          *
 ***************************************************************************/
using System;

namespace NVorbis
{
    /// <summary>
    /// Event data for when a new logical stream is found in a container.
    /// </summary>
    [Serializable]
    public class NewStreamEvent
    {
        /// <summary>
        /// Gets new the <see cref="IPacketProvider"/> instance.
        /// </summary>
        public IPacketProvider PacketProvider { get; }

        /// <summary>
        /// Gets or sets whether to ignore the logical stream associated with the packet provider.
        /// </summary>
        public bool IgnoreStream { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="NewStreamEvent"/> with the specified <see cref="IPacketProvider"/>.
        /// </summary>
        /// <param name="packetProvider">An <see cref="IPacketProvider"/> instance.</param>
        public NewStreamEvent(IPacketProvider packetProvider)
        {
            PacketProvider = packetProvider ?? throw new ArgumentNullException(nameof(PacketProvider));
        }
    }
}