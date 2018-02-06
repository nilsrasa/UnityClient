using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using uint8 = System.Byte;
using Messages.geometry_msgs;
using Messages.sensor_msgs;
using Messages.actionlib_msgs;

using Messages.std_msgs;
using String=System.String;

namespace Messages.fiducial_msgs
{
#if !TRACE
    [System.Diagnostics.DebuggerStepThrough]
#endif
    public class FiducialMapEntryArray : IRosMessage
    {

			public Messages.fiducial_msgs.FiducialMapEntry[] fiducials;


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string MD5Sum() { return "f3d7e1cb2717bda61be54acdb77f4f79"; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool HasHeader() { return false; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool IsMetaType() { return true; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string MessageDefinition() { return @"FiducialMapEntry[] fiducials"; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override MsgTypes msgtype() { return MsgTypes.fiducial_msgs__FiducialMapEntryArray; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool IsServiceComponent() { return false; }

        [System.Diagnostics.DebuggerStepThrough]
        public FiducialMapEntryArray()
        {
            
        }

        [System.Diagnostics.DebuggerStepThrough]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public FiducialMapEntryArray(byte[] SERIALIZEDSTUFF)
        {
            Deserialize(SERIALIZEDSTUFF);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public FiducialMapEntryArray(byte[] SERIALIZEDSTUFF, ref int currentIndex)
        {
            Deserialize(SERIALIZEDSTUFF, ref currentIndex);
        }



        [System.Diagnostics.DebuggerStepThrough]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override void Deserialize(byte[] SERIALIZEDSTUFF, ref int currentIndex)
        {
            int arraylength=-1;
            bool hasmetacomponents = false;
            object __thing;
            int piecesize=0;
            byte[] thischunk, scratch1, scratch2;
            IntPtr h;
            
            //fiducials
            hasmetacomponents |= true;
            arraylength = BitConverter.ToInt32(SERIALIZEDSTUFF, currentIndex);
            currentIndex += Marshal.SizeOf(typeof(System.Int32));
            if (fiducials == null)
                fiducials = new Messages.fiducial_msgs.FiducialMapEntry[arraylength];
            else
                Array.Resize(ref fiducials, arraylength);
            for (int i=0;i<fiducials.Length; i++) {
                //fiducials[i]
                fiducials[i] = new Messages.fiducial_msgs.FiducialMapEntry(SERIALIZEDSTUFF, ref currentIndex);
            }
        }

        [System.Diagnostics.DebuggerStepThrough]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override byte[] Serialize(bool partofsomethingelse)
        {
            int currentIndex=0, length=0;
            bool hasmetacomponents = false;
            byte[] thischunk, scratch1, scratch2;
            List<byte[]> pieces = new List<byte[]>();
            GCHandle h;
            
            //fiducials
            hasmetacomponents |= true;
            if (fiducials == null)
                fiducials = new Messages.fiducial_msgs.FiducialMapEntry[0];
            pieces.Add(BitConverter.GetBytes(fiducials.Length));
            for (int i=0;i<fiducials.Length; i++) {
                //fiducials[i]
                if (fiducials[i] == null)
                    fiducials[i] = new Messages.fiducial_msgs.FiducialMapEntry();
                pieces.Add(fiducials[i].Serialize(true));
            }
            //combine every array in pieces into one array and return it
            int __a_b__f = pieces.Sum((__a_b__c)=>__a_b__c.Length);
            int __a_b__e=0;
            byte[] __a_b__d = new byte[__a_b__f];
            foreach(var __p__ in pieces)
            {
                Array.Copy(__p__,0,__a_b__d,__a_b__e,__p__.Length);
                __a_b__e += __p__.Length;
            }
            return __a_b__d;
        }

        public override void Randomize()
        {
            int arraylength=-1;
            Random rand = new Random();
            int strlength;
            byte[] strbuf, myByte;
            
            //fiducials
            arraylength = rand.Next(10);
            if (fiducials == null)
                fiducials = new Messages.fiducial_msgs.FiducialMapEntry[arraylength];
            else
                Array.Resize(ref fiducials, arraylength);
            for (int i=0;i<fiducials.Length; i++) {
                //fiducials[i]
                fiducials[i] = new Messages.fiducial_msgs.FiducialMapEntry();
                fiducials[i].Randomize();
            }
        }

        public override bool Equals(IRosMessage ____other)
        {
            if (____other == null) return false;
            bool ret = true;
            fiducial_msgs.FiducialMapEntryArray other = (Messages.fiducial_msgs.FiducialMapEntryArray)____other;

            if (fiducials.Length != other.fiducials.Length)
                return false;
            for (int __i__=0; __i__ < fiducials.Length; __i__++)
            {
                ret &= fiducials[__i__].Equals(other.fiducials[__i__]);
            }
            // for each SingleType st:
            //    ret &= {st.Name} == other.{st.Name};
            return ret;
        }
    }
}
