using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pcsdk4Explorer
{
    class FacePosition
    {
        public FacePosition(PXCMPoint3DF32 LeftEye, PXCMPoint3DF32 RightEye, PXCMPoint3DF32 Mouth/*, PXCMPoint3DF32 CenterFrame*/)
        {
            mLeftEye = LeftEye;
            mRightEye = RightEye;
            mMouth = Mouth;
            //mCenterFrame = CenterFrame;
            // angle between eyes
            mAngleLeftEyeToRightEye = (int)CalculateAngle(mLeftEye, mRightEye);
            // angle between eye - mouth
            mAngleLeftEyeToMouth = (int)CalculateAngle(mLeftEye, mMouth);
            mAngleRightEyeToMouth = (int)CalculateAngle(mRightEye, mMouth);
            //center face
            mCenterFace.x = (mLeftEye.x + mRightEye.x) / 2;
            mCenterFace.y = (mLeftEye.y + mRightEye.y) / 2;

            mEyesDistance = getDistance(mLeftEye, mRightEye);
        }

        protected PXCMPoint3DF32 mLeftEye;
        protected PXCMPoint3DF32 mRightEye;
        protected PXCMPoint3DF32 mMouth;
        //protected PXCMPoint3DF32 mCenterFrame;
        protected PXCMPoint3DF32 mCenterFace;
        int mAngleLeftEyeToRightEye;
        int mAngleLeftEyeToMouth;
        int mAngleRightEyeToMouth;
        int mEyesDistance;
        
        protected static double CalculateAngle(PXCMPoint3DF32 point1, PXCMPoint3DF32 point2)
        {
            int dx = (int)point1.x - (int)point2.x;
            int dy = (int)point2.y - (int)point1.y;
            return Math.Atan((double)dy / (double)dx) * 180 / 3.14;
        }
        protected static int getDistance(PXCMPoint3DF32 point1, PXCMPoint3DF32 point2)
        {
            return (int)Math.Sqrt((double)((point1.x - point2.x) * (point1.x - point2.x) + (point1.y - point2.y) * (point1.y - point2.y)));
        }
        public int getCenter()
        {
            return (int)mCenterFace.x;
        }
        public string m_incline_to;
        public string m_turn_to;
        public string m_z_to;
        public int getAndleEyes() { return mAngleLeftEyeToRightEye; }
	    //public int getCenter() { return (int)mCenterFace.x; }
	    public int getEyesDist() { return mEyesDistance; }
    }

    class GestureDetector
    {
        int MAX_FRAMES_COUNT = 15;

        //threaf safe
        public static GestureDetector instance = new GestureDetector();
        private GestureDetector() { }
        public static GestureDetector Instance
        {
            get
            {
                return instance;
            }
        }
        //private object _lock;
        public void Clear()
        {
            mFacePositionsSequence.Clear();
        }

        public void AddPosition(FacePosition FacePosition)
        {
            mFacePositionsSequence.Add(FacePosition);
            if (mFacePositionsSequence.Count >= MAX_FRAMES_COUNT)
            {
                mFacePositionsSequence.RemoveAt(0);
            }
        }

        public int m_Detected_Gesture = 0;
        public void Process(out int result, out string str1, out string str2)
        {
            str1 = "";
            str2 = "";
            result = mFacePositionsSequence.Count;
            
            // incline delta
            if (mFacePositionsSequence.Count < 2)
                return;

            // incline amplitude & history
            int angle_eyes_prelast = mFacePositionsSequence[mFacePositionsSequence.Count - 2].getAndleEyes();
            int angle_eyes_last = mFacePositionsSequence[mFacePositionsSequence.Count - 1].getAndleEyes();
            int delta_angle_eyes = angle_eyes_last - angle_eyes_prelast;
            if (delta_angle_eyes < 0)
                mFacePositionsSequence[mFacePositionsSequence.Count - 1].m_incline_to = "<";
            if (delta_angle_eyes > 0)
                mFacePositionsSequence[mFacePositionsSequence.Count - 1].m_incline_to = ">";
            if (delta_angle_eyes == 0)
                mFacePositionsSequence[mFacePositionsSequence.Count - 1].m_incline_to = ".";
            if (Math.Abs(delta_angle_eyes) > 100)
                mFacePositionsSequence[mFacePositionsSequence.Count - 1].m_incline_to = "E";

            // turn amplitude & history
            int center_face_turn_prelast = mFacePositionsSequence[mFacePositionsSequence.Count - 2].getCenter();
            int center_face_turn_last = mFacePositionsSequence[mFacePositionsSequence.Count - 1].getCenter();
            int delta_center_face_turn = center_face_turn_last - center_face_turn_prelast;
            if (delta_center_face_turn < 0)
                mFacePositionsSequence[mFacePositionsSequence.Count - 1].m_turn_to = "<";
            if (delta_center_face_turn > 0)
                mFacePositionsSequence[mFacePositionsSequence.Count - 1].m_turn_to = ">";
            if (delta_center_face_turn == 0)
                mFacePositionsSequence[mFacePositionsSequence.Count - 1].m_turn_to = ".";
            if (Math.Abs(delta_center_face_turn) > 100)
                mFacePositionsSequence[mFacePositionsSequence.Count - 1].m_turn_to = "E";

            // z movement history
            int eyes_dist_prelast = mFacePositionsSequence[mFacePositionsSequence.Count - 2].getEyesDist();
            int eyes_dist_last = mFacePositionsSequence[mFacePositionsSequence.Count - 1].getEyesDist();
            int delta_eyes_dist = eyes_dist_last - eyes_dist_prelast;
            if (delta_eyes_dist < 0)
                mFacePositionsSequence[mFacePositionsSequence.Count - 1].m_z_to = "o";
            if (delta_eyes_dist > 0)
                mFacePositionsSequence[mFacePositionsSequence.Count - 1].m_z_to = "O";
            if (delta_eyes_dist == 0)
                mFacePositionsSequence[mFacePositionsSequence.Count - 1].m_z_to = ".";
            if (Math.Abs(delta_eyes_dist) > 100)
                mFacePositionsSequence[mFacePositionsSequence.Count - 1].m_z_to = "E";

            CalcAmplitudes();
            CalcHistory();

            string sequence_hor = "";
            string sequence_round = "";
            for (int i = 0; i < mFacePositionsSequence.Count; i++)
            {
                sequence_hor += mFacePositionsSequence[i].m_turn_to;
                sequence_round += mFacePositionsSequence[i].m_incline_to;
            }
            str1 = sequence_hor;
            str2 = sequence_round;
        }
        public int GetResult()
        {
            return m_Detected_Gesture;
        }
        List<FacePosition> mFacePositionsSequence = new List<FacePosition>();

        //===
        protected void CalcHistory()
	    {
		    mInclineHistory = "";
            mTurnHistory = "";
            mZHistory = "";
		    for(int i = 0; i < mFacePositionsSequence.Count; i++)
		    {
			    mInclineHistory	+= mFacePositionsSequence[i].m_incline_to;
			    mTurnHistory	+= mFacePositionsSequence[i].m_turn_to;
			    mZHistory		+= mFacePositionsSequence[i].m_z_to;
		    }
	    }
        protected int CalcAmplitudes()
	    {
		    if( mFacePositionsSequence.Count < 2 )
			    return -1;
		    int angle_min = mFacePositionsSequence[0].getAndleEyes(), angle_max = mFacePositionsSequence[0].getAndleEyes();
		    int turn_min = mFacePositionsSequence[0].getCenter(), turn_max = mFacePositionsSequence[0].getCenter();


            int horizontal_delta = 0;

		    for( int i = 0; i < mFacePositionsSequence.Count; i++)
		    {
			    int angle_ = mFacePositionsSequence[i].getAndleEyes();
			    if(angle_ < angle_min) angle_min = angle_;
			    if(angle_ > angle_max) angle_max = angle_;

                if (i > 0)
                {
                    int d = mFacePositionsSequence[i].getCenter() - mFacePositionsSequence[i - 1].getCenter();
                    horizontal_delta += d;
                }
		    }
		    mAmplitudeIncline = angle_max - angle_min;
            mAmplitudeTurnHorizontal = horizontal_delta;

            return 0;
	    }
        //===
        string mInclineHistory;
	    string mTurnHistory;
	    string mZHistory;

        public int mAmplitudeTurnHorizontal;
        public int mAmplitudeTurnHorizontal_center=0;

	    public int mAmplitudeIncline;
	    int mAmplitudeUpDown;
	    int mAmplitudeZdistance;

    }
}
