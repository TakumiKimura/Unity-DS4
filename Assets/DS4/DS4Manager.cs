using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Runtime.InteropServices;

namespace DS4Api
{

    public static class DS4Manager
    {
        public static readonly ushort[] pids = new ushort[]
        {
            0x05C4, // CUH-ZCT1
            0x09CC, // CUH-ZCT2
        };

        public const ushort vid = 0x054C;

        /// A list of all currently connected DS4s.
        public static ReadOnlyCollection<DS4> Controllers
        {
            get
            {
                if(_Controllers == null) { return null; }
                return _Controllers.AsReadOnly();
            }
        }
        private static List<DS4> _Controllers = new List<DS4>();

        public static bool FindDS4s()
        {
            IntPtr ptr = IntPtr.Zero;

            foreach(var pid in pids)
            {
                ptr = HIDapi.hid_enumerate(vid, pid);
                if(ptr != IntPtr.Zero)
                {
                    break;
                }
            }
            
            if (ptr == IntPtr.Zero)
            {
                return false;
            }

            IntPtr cur_ptr = ptr;

            hid_device_info enumerate = (hid_device_info)Marshal.PtrToStructure(ptr, typeof(hid_device_info));

            bool found = false;

            while (cur_ptr != IntPtr.Zero)
            {
                DS4 remote = null;
                
                foreach (DS4 r in Controllers)
                {
                    if (r.hidapi_path.Equals(enumerate.path))
                    {
                        remote = r;
                        break;
                    }
                }

                if (remote == null)
                {
                    IntPtr handle = HIDapi.hid_open_path(enumerate.path);

                    remote = new DS4(handle, enumerate.path);

                    Debug.Log("Found New Remote: " + remote.hidapi_path);

                    _Controllers.Add(remote);

                    // TODO: Initialization (?)
                }

                cur_ptr = enumerate.next;
                if(cur_ptr != IntPtr.Zero)
                {
                    enumerate = (hid_device_info)Marshal.PtrToStructure(cur_ptr, typeof(hid_device_info));
                }
            }

            HIDapi.hid_free_enumeration(ptr);

            return found;
        }

        public static void Cleanup(DS4 remote)
        {
            if (remote != null)
            {
                if (remote.hidapi_handle != IntPtr.Zero)
                    HIDapi.hid_close(remote.hidapi_handle);

                _Controllers.Remove(remote);
            }
        }

        public static bool HasWiimote()
        {
            return !(Controllers.Count <= 0 || Controllers[0] == null || Controllers[0].hidapi_handle == IntPtr.Zero);
        }
    }
}