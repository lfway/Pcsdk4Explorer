using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BHO_HelloWorld
{
    class FacePosition
    {
        FacePosition(PXCMPoint3DF32 LeftEye, PXCMPoint3DF32 RightEye, PXCMPoint3DF32 Mouth/*, PXCMPoint3DF32 CenterFrame*/)
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
    }

    class GestureDetector
    {
        protected FacePosition mFacePosition;
        void AddPosition(FacePosition FacePosition)
        {
            mFacePosition = FacePosition;
        }
        void Process()
        {
            if (mFacePositionsSequence.Count == 1)
            {
                ////mAmplitudeTurnHorizontal_center = mFacePositionsSequence[0].getCenter();
                int qwe = 234;
            }
        }
        List<FacePosition> mFacePositionsSequence;
        int mSecuenceLength;
    }
}
