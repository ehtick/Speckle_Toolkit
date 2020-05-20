﻿/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2019, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BH.oM.Adapter;
using BH.oM.Base;
using BH.oM.Data.Requests;
using SpeckleCore;
using BH.oM.Speckle;
using System.ComponentModel;
using BH.Engine.Base;
using System.Collections.Concurrent;

namespace BH.Adapter.Speckle
{
    public partial class SpeckleAdapter
    {
        public override List<object> Push(IEnumerable<object> objects, string tag = "", PushType pushType = PushType.AdapterDefault, ActionConfig actionConfig = null)
        {
            // Clone objects for immutability in the UI
            List<object> objectsToPush = objects.Select(x => x.DeepClone()).ToList();

            // Initialize the SpeckleStream
            SpeckleStream.Objects = new List<SpeckleObject>(); // stream is immutable
            SpeckleClient.StreamId = SpeckleClient.StreamId ?? SpeckleStreamId; // make sure it's there

            // //- Read config
            SpecklePushConfig pushConfig = (actionConfig as SpecklePushConfig) ?? new SpecklePushConfig();

            // //- Use "Speckle" history: produces a new stream at every push that corresponds to the old version. Enabled by default.
            if (pushConfig.EnableHistory)
                SetupHistory();

            // Actual creation and add to the stream
            for (int i = 0; i < objectsToPush.Count(); i++)
            {
                SpeckleObject speckleObject = Create(objectsToPush[i] as dynamic, pushConfig); // Dynamic dispatch to most appropriate method

                // Add objects to the stream
                SpeckleStream.Objects.Add(speckleObject);
            }

            // Send the objects
            try
            {
                // Issue: with `StreamUpdateAsync` Speckle doesn't seem to send anything if the Stream is initially empty.
                // You need to Push twice if the Stream is initially empty.
                //var updateResponse = SpeckleClient.StreamUpdateAsync(SpeckleStreamId, SpeckleStream).Result;
                //SpeckleClient.BroadcastMessage("stream", SpeckleStreamId, new { eventType = "update-global" });
                UpdateStream(pushConfig);
            }
            catch (Exception e)
            {
                BH.Engine.Reflection.Compute.RecordError($"Upload to Speckle failed. Message returned:\n{e.Message}");
                return new List<object>();
            }

            return objectsToPush;
        }


    
    }
}
