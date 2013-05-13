using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BHO_HelloWorld
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
        int mEyesDist;
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
	    public int getEyesDist() { return mEyesDist; }
    }

    class GestureDetector
    {
        protected FacePosition mFacePosition;
        public void AddPosition(FacePosition FacePosition)
        {
            mFacePosition = FacePosition;
        }
        public int m_Detected_Gesture = 0;
        public void Process()
        {
            if (mFacePositionsSequence.Count == 1)
            {
                mAmplitudeTurnHorizontal_center = mFacePositionsSequence[0].getCenter();       
            }
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
            for (int i = 0; i < mFacePositionsSequence.Count; i++)
            {
                sequence_hor += mFacePositionsSequence[i].m_turn_to;
            }
            int qwe = sequence_hor.IndexOf("<<<<");
            if (qwe != -1)
                m_Detected_Gesture = 1;
            else
                m_Detected_Gesture = 0;
        }
        public int GetResult()
        {
            return m_Detected_Gesture;
        }
        List<FacePosition> mFacePositionsSequence;
        int mSecuenceLength;
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
		    for( int i = 0; i < mFacePositionsSequence.Count; i++)
		    {
			    int angle_ = mFacePositionsSequence[i].getAndleEyes();
			    if(angle_ < angle_min) angle_min = angle_;
			    if(angle_ > angle_max) angle_max = angle_;

			    //int turn_ = mFacePositionsSequence[i].getCenter() - mAmplitudeTurnHorizontal_center;
                int turn_ = Math.Abs(mAmplitudeTurnHorizontal_center - Math.Abs(mAmplitudeTurnHorizontal_center - mFacePositionsSequence[i].getCenter()));
			    if(turn_ < turn_min) turn_min = turn_;
			    if(turn_ > turn_max) turn_max = turn_;
		    }
		    mAmplitudeIncline = angle_max - angle_min;
            mAmplitudeTurnHorizontal = Math.Abs(Math.Abs(turn_max + turn_min) / 2 - mAmplitudeTurnHorizontal_center);

            if (Math.Abs(mAmplitudeTurnHorizontal) < 3)
			    mAmplitudeTurnHorizontal_center = (turn_max + turn_min)/2;

		    //mAmplitudeTurnHorizontal; -= mAmplitudeTurnHorizontal_center;
            return 0;
	    }
        //===
        string mInclineHistory;
	    string mTurnHistory;
	    string mZHistory;

	    int mAmplitudeTurnHorizontal;
	    int mAmplitudeTurnHorizontal_center;

	    int mAmplitudeIncline;
	    int mAmplitudeUpDown;
	    int mAmplitudeZdistance;

    }
}
