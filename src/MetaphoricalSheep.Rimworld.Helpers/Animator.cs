using System;
using System.Collections.Generic;

namespace MetaphoricalSheep.Rimworld.Helpers
{
    public class Animator
    {
        /// <summary>
        /// Do not animate, return false immediately
        /// </summary>
        public bool DisableAnimation = false;

        /// <summary>
        /// First frame of the array of textures
        /// </summary>
        public int FirstFrame = 0;

        /// <summary>
        /// Number of frames in the array of textures
        /// </summary>
        public int Frames;

        /// <summary>
        /// New frame will be calculated ever N ticks
        /// </summary>
        public int UpdateEveryNTicks = 5;

        /// <summary>
        /// Extended frames (key) will wait longer (value * UpdateEveryNTicks) before calculating next frame.
        /// Not affected by Arcing animation
        /// </summary>
        public Dictionary<int, int> ExtendedFrames = new Dictionary<int, int>();

        /// <summary>
        /// Animation type, defaults to Loop.
        /// </summary>
        public Animation AnimationType = Animation.Loop;

        /// <summary>
        /// --- Arc Animation only ---
        /// Minimum number of ticks to wait on static frame for Arcing animation
        /// </summary>
        public int MinStaticArcWaitTicks = 60;  // 1 second

        /// <summary>
        /// --- Arc Animation only ---
        /// Maximum number of ticks to wait on static frame for Arcing animation
        /// </summary>
        public int MaxStaticArcWaitTicks = 600; // 10 seconds

        /// <summary>
        /// --- Arc Animation only ---
        /// If set to true, arc frames will cycle through random frames every
        /// ArcCycleFramesEveryNTicks ticks ticks while waiting for
        /// UpdateEveryNTicks to be reached
        /// </summary>
        public bool ArcAnimationShouldCycle = true;

        /// <summary>
        /// --- Arc Animation only ---
        /// Cycle through arc frames every n ticks
        /// </summary>
        public int ArcCycleFramesEveryNTicks = 5;
        
        /// <summary>
        /// The current frame that should be played
        /// </summary>
        public int CurrentFrame;



        /// <summary>
        /// Number of ticks that has passed since the last frame calculation
        /// </summary>
        private int _ticksSinceLastUpdate;

        /// <summary>
        /// The previous frame that was played
        /// </summary>
        private int _previousFrame;

        /// <summary>
        /// The number of times UpdateEveryNTicks has been reached for the current extended frame
        /// </summary>
        private int _extendedTickDuration;

        /// <summary>
        /// --- Arc Animation only ---
        /// Number of ticks to play static frame for when animation type is Arcing.
        /// Will randomize between Min- and MaxStaticArcWaitTicks
        /// </summary>
        private int _arcStaticUpdateEveryNTicks;

        /// <summary>
        /// Helper property to return last frame
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private int _lastFrame => Frames - 1;

        public enum Animation
        {
            /// <summary>
            /// Restart at first frame after looping through frames
            /// </summary>
            Loop,
            /// <summary>
            /// Start slow, ramp up, then slow down animation
            /// </summary>
            SpeedUp,
            /// <summary>
            /// Same as loop, but loop back down to first frame
            /// </summary>
            Yoyo,
            /// <summary>
            /// Play a random frame, excluding the current frame
            /// </summary>
            Random,
            /// <summary>
            /// Play the first frame for random ticks between Min- and MaxStaticArcWaitTicks
            /// Then cycle through random frames (if enabled) for UpdateEveryNTicks
            /// </summary>
            Arcing,
        }

        /// <summary>
        /// Gets called by thing ticker method.
        /// Decides what frame needs to be played based on tick duration
        /// and other rules depending on animation type.
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns>Bool indicating whether the frame needs to be updated</returns>
        public bool DoTickerWork(int ticks)
        {
            // If animations are disabled do nothing
            if (DisableAnimation)
            {
                return false;
            }

            // increase the number of ticks since the last time
            _ticksSinceLastUpdate += ticks;

            // Break out for custom arc timing
            if (AnimationType == Animation.Arcing)
            {
                return DoArcingTickerWork();
            }

            // if number of ticks passed is less than the number of ticks for animation do nothing
            if (_ticksSinceLastUpdate < UpdateEveryNTicks)
            {
                return false;
            }

            // reset number of ticks waited to 0
            _ticksSinceLastUpdate = 0;

            // check if this is an extended frame
            if (ExtendedFrames.ContainsKey(CurrentFrame))
            {
                var duration = ExtendedFrames[CurrentFrame];

                // extended duration is not done yet
                if (_extendedTickDuration != duration)
                {
                    _extendedTickDuration++;
                    return false;
                }

                _extendedTickDuration = 0;
            }

            // calculate the next frame
            CalculateNextFrame();

            return true;
        }

        /// <summary>
        /// Make sure the tick values are valid
        /// </summary>
        private void ValidateStaticArcTicks()
        {
            // Validate Min- and MaxArcWaitTicks
            MinStaticArcWaitTicks = (MinStaticArcWaitTicks <= 0) ? 1 : MinStaticArcWaitTicks;
            MaxStaticArcWaitTicks = (MaxStaticArcWaitTicks <= 0) ? 1 : MaxStaticArcWaitTicks;

            if (MaxStaticArcWaitTicks < MinStaticArcWaitTicks)
            {
                var temp = MaxStaticArcWaitTicks;
                MaxStaticArcWaitTicks = MinStaticArcWaitTicks;
                MinStaticArcWaitTicks = temp;
            }
        }

        /// <summary>
        /// Get the next number of ticks that the static arc frame needs to be played for
        /// </summary>
        private void SetupArcUpdateTicks()
        {
            // already set, ignore
            if (_arcStaticUpdateEveryNTicks != 0)
            {
                return;
            }

            ValidateStaticArcTicks();

            // Get next arc ticks
            _arcStaticUpdateEveryNTicks = Random.Next(MinStaticArcWaitTicks, MaxStaticArcWaitTicks + 1);
        }

        /// <summary>
        /// Custom ticker mehtod for arc animation
        /// </summary>
        /// <returns>Bool indicating whether the frame needs to be updated</returns>
        private bool DoArcingTickerWork()
        {
            SetupArcUpdateTicks();

            // if static frame and static ticks are not over, do nothing
            if (CurrentFrame == 0 && _ticksSinceLastUpdate < _arcStaticUpdateEveryNTicks)
            {
                // no frame update
                return false;
            }

            // if not static frame and ticks are not over
            if (_ticksSinceLastUpdate < UpdateEveryNTicks)
            {
                // if not cycle, or not time to cycle, do nothing
                if (!ArcAnimationShouldCycle || _ticksSinceLastUpdate % ArcCycleFramesEveryNTicks <= 0)
                {
                    // no frame update
                    return false;
                }

                // get next cycle frame
                ArcingAnimation(true);
                return true;
            }

            // reset number of ticks waited to 0
            _ticksSinceLastUpdate = 0;
            // reset arc frame ticks so that we can get a new random delay
            _arcStaticUpdateEveryNTicks = 0;

            // calculate the next frame
            CalculateNextFrame();

            return true;
        }


        /// <summary>
        /// Calculate the next frame based on the animation type
        /// </summary>
        private void CalculateNextFrame()
        {
            var previous = _previousFrame;
            _previousFrame = CurrentFrame;

            switch (AnimationType)
            {
                case Animation.Loop:
                    LoopAnimation();
                    break;
                case Animation.SpeedUp:
                    SpeedUpAnimation();
                    break;
                case Animation.Yoyo:
                    YoYoAnimation(previous);
                    break;
                case Animation.Random:
                    RandomAnimation();
                    break;
                case Animation.Arcing:
                    ArcingAnimation();
                    break;
                default:
                    LoopAnimation();
                    break;
            }
        }

        /// <summary>
        /// Play every frame in order. Start again at frame 0 when last frame is played.
        /// </summary>
        private void LoopAnimation()
        {
            // if last frame has been played
            if (CurrentFrame == _lastFrame)
            {
                // go back to first frame
                CurrentFrame = FirstFrame;
                return;
            }

            // else go to next frame
            CurrentFrame++;
        }

        /// <summary>
        /// Start slow, speed up, then speed back down
        /// </summary>
        private void SpeedUpAnimation()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Play to last frame, then play in reverse order back to first frame. Repeat
        /// </summary>
        /// <param name="previous">The previous frame</param>
        private void YoYoAnimation(int previous)
        {
            // if not last frame played
            if (CurrentFrame != _lastFrame && CurrentFrame > previous || CurrentFrame == 0)
            {
                // play next frame
                CurrentFrame++;
                return;
            }

            // play previous frame
            CurrentFrame--;
        }

        /// <summary>
        /// Set the current frame to a random frame between FirstFrame and Frames, excluding CurrentFrame
        /// </summary>
        private void RandomAnimation()
        {
            CurrentFrame = Random.RandomExcluding(FirstFrame, Frames, CurrentFrame);
        }

        /// <summary>
        /// Play the first frame for random ticks between Min- and MaxStaticArcWaitTicks
        /// Then cycle through random frames (if enabled) for UpdateEveryNTicks
        /// </summary>
        private void ArcingAnimation(bool cycle = false)
        {
            if (!cycle)
            {
                // if previous frame was static, then get animation frame, else get static frame
                CurrentFrame = (CurrentFrame != 0) ? 0 : Random.RandomExcluding(FirstFrame + 1, Frames, FirstFrame);
                return;
            }

            // if cycle then get a random frame, excluding first frame
            CurrentFrame = Random.RandomExcluding(FirstFrame + 1, Frames + 1, FirstFrame);
        }
    }
}