using System;
using System.Collections.Generic;
using OpenBveApi.Runtime;
using System.Security.Cryptography;

namespace Plugin
{
    class Windscreen : Device
    {
        /// <summary>The underlying train.</summary>
        private Train Train;
        // --- members ---
        internal bool enabled;
        //Internal Variables
        internal bool israining;
        internal int rainintensity;
        internal int droptimer;
        internal int nextdrop;
        internal int wiperspeed = 0;
        internal int currentwiperposition;
        internal int wiperdirection;
        internal double wipermovetimer;
        internal bool heldwipers;
        internal double wiperheldtimer;

        //Default Variables
        internal int dropstartindex = 0;
        internal int numberofdrops = 0;
        internal int wiperindex = -1;
        internal double wiperholdposition = 0;
        internal double wiperrate = 1000;
        internal double wiperdelay = 0;
        internal int dropsound1 = -1;
        internal int dropsound2 = 0;
        internal int drywipesound = -1;
        internal int wetwipesound = -1;
        internal int wipersoundbehaviour = 0;
        internal int wiperswitchindex = -1;
        internal int wiperswitchsound = -1;
        
        //Arrays
        bool[] droparray;

        internal Windscreen(Train train)
        {
            this.Train = train;
        }

        internal override void Initialize(InitializationModes mode)
        {
            if (numberofdrops != 0)
            {
                //Create arrays with number of drops
                droparray = new bool[numberofdrops];
            }
            currentwiperposition = (int)wiperholdposition;
        }

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            Random rnd = new Random();
            //Is rain and windscreen wipers enabled?
            if (this.enabled)
            {
                //First pull a random unlit drop from our array
                List<int> unuseddrops = new List<int>();
                unuseddrops.Clear();
                int count = 0;
                int unusedlength = 0;
                foreach (bool x in droparray)
                {
                    count++;
                    if (x == false)
                    {
                        //If this drop is not yet lit, add it's index to the unused drops
                        unuseddrops.Add(count);
                        unusedlength++;
                    }
                }
                if (count == 0)
                {
                    //Nothing to pick
                    nextdrop = -1;
                }
                else
                {
                    //Pick a random drop from the unused drops
                    nextdrop = unuseddrops[rnd.Next(0, unusedlength - 1)];
                }

                //If we're raining, 
                if (israining == true)
                {
                    //Generate a random drop interval
                    int dev = (int)(0.4 * 2000 / rainintensity);
                    int dropinterval = (2000 / rainintensity) + (rnd.Next(dev, dev * 2));

                    droptimer += (int)data.ElapsedTime.Milliseconds;
                    //If we're past the drop interval, try to add a drop
                    if (droptimer > dropinterval && nextdrop != -1)
                    {
                        droptimer = 0;
                        droparray[nextdrop] = true;
                        //Play random drop sound
                        if (dropsound1 != -1)
                        {
                            SoundManager.Play((dropsound1 + rnd.Next(0, dropsound2)), 1.0, 1.0, false);
                        }
                    }
                    else if (droptimer > dropinterval && nextdrop == -1)
                    {
                        //Reset timer and play random drop sound
                        droptimer = 0;
                        if (dropsound1 != -1)
                        {
                            SoundManager.Play((dropsound1 + rnd.Next(0, dropsound2)), 1.0, 1.0, false);
                        }
                    }

                }

                //This section of code moves our windscreen wipers
                if (currentwiperposition > 0 && currentwiperposition < 100)
                {
                    //Always move wiper if not at rest- No need to set direction
                    movewiper(data.ElapsedTime.Milliseconds);
                    wiperheldtimer = 0.0;
                }
                else if (currentwiperposition == 0 && currentwiperposition == wiperholdposition)
                {
                    //Set new direction
                    wiperdirection = 1;
                    if (wiperspeed == 0)
                    {
                        heldwipers = true;
                    }
                    else if (wiperspeed == 1)
                    {
                        wiperheldtimer += data.ElapsedTime.Milliseconds;
                        if (wiperheldtimer > wiperdelay)
                        {
                            heldwipers = false;
                            wiperheldtimer = 0.0;
                        }
                        else
                        {
                            heldwipers = true;
                        }
                    }
                    else
                    {
                        heldwipers = false;
                    }

                    if (heldwipers == false)
                    {
                        movewiper(data.ElapsedTime.Milliseconds);
                    }

                }
                else if (currentwiperposition == 0 && currentwiperposition != wiperholdposition)
                {
                    //Set new direction
                    wiperdirection = 1;
                    movewiper(data.ElapsedTime.Milliseconds);
                }
                else if (currentwiperposition == 100 && currentwiperposition != wiperholdposition)
                {
                    //Set new direction
                    wiperdirection = -1;
                    movewiper(data.ElapsedTime.Milliseconds);
                }
                else if (currentwiperposition == 100 && currentwiperposition == wiperholdposition)
                {
                    //Set new direction
                    wiperdirection = -1;
                    if (wiperspeed == 0)
                    {
                        heldwipers = true;
                    }
                    else if (wiperspeed == 1)
                    {
                        wiperheldtimer += data.ElapsedTime.Milliseconds;
                        if (wiperheldtimer > wiperdelay)
                        {
                            heldwipers = false;
                            wiperheldtimer = 0.0;
                        }
                        else
                        {
                            heldwipers = true;
                        }
                    }
                    else
                    {
                        heldwipers = false;
                    }

                    if (heldwipers == false)
                    {
                        movewiper(data.ElapsedTime.Milliseconds);
                    }
                }
                else
                {
                    //Set new direction
                    wiperdirection = 1;
                    movewiper(data.ElapsedTime.Milliseconds);
                }

                //This section of code plays the wiper sounds
                {
                    int sound;
                    //Figure out if we should play the wetwipe or the drywipe sound
                    if ((double)unusedlength / (double)droparray.Length > 0.8 && wetwipesound != 1)
                    {
                        sound = drywipesound;
                    }
                    else
                    {
                        sound = wetwipesound;
                    }
                    if (currentwiperposition == 1 && wiperdirection == 1)
                    {
                        if (wiperholdposition == 0)
                        {
                            if (wipersoundbehaviour == 0)
                            {
                                SoundManager.Play(sound, 1.0, 1.0, false);
                            }
                        }
                        else
                        {
                            if (wipersoundbehaviour != 0)
                            {
                                SoundManager.Play(sound, 1.0, 1.0, false);
                            }
                        }
                    }
                    else if (currentwiperposition == 99 && wiperdirection == -1)
                    {
                        if (wiperholdposition == 0)
                        {
                            if (wipersoundbehaviour != 0)
                            {
                                SoundManager.Play(sound, 1.0, 1.0, false);
                            }
                        }
                        else
                        {
                            if (wipersoundbehaviour == 0)
                            {
                                SoundManager.Play(sound, 1.0, 1.0, false);
                            }
                        }
                    }
                }

                //This section of code should delete drops
                if (heldwipers == false)
                {
                    int dropremove = Math.Min(49, (int)(currentwiperposition / (100 / numberofdrops)));
                    droparray[dropremove] = false;
                }

                //Light Windscreen Drops
                {
                    int i = 0;
                    foreach (bool x in droparray)
                    {
                        i++;
                        if (x == true)
                        {
                            this.Train.Panel[(i + dropstartindex - 1)] = 1;
                        }
                    }
                }
                //Animate Windscreen Wiper
                if (wiperindex != -1)
                {
                    this.Train.Panel[wiperindex] = currentwiperposition;
                }
                //Animate Windscreen Wiper Switch
                if (wiperswitchindex != -1)
                {
                    this.Train.Panel[wiperswitchindex] = wiperspeed;
                }
            }
            

        }

        /// <summary>Called to move the windscreen wiper</summary>
        /// <param name="time">The time elapsed since the previous call.</param>
        internal void movewiper(double time)
        {
            wipermovetimer += time;
            if (wipermovetimer > (wiperrate / 100))
            {
                wipermovetimer = 0.0;
                if (wiperdirection == 1)
                {
                    currentwiperposition++;
                }
                else
                {
                    currentwiperposition--;
                }
            }
        }

        /// <summary>Call this from the beacon reciever to start or stop rain falling</summary>
        internal void rainstart(int intensity)
        {
            rainintensity = intensity;
            if (israining == false && intensity != 0)
            {
                israining = true;
            }
            else
            {
                israining = false;
            }
        }

        /// <summary>Called to speed up or slow down the windscreen wipers</summary>
        /// <param name="request">Whether to speed up or slow down the wipers; 1 for speed up & 0 for slow down.</param>
        internal void windscreenwipers(int request)
        {
            if (request == 0 && wiperspeed <= 1)
            {
                wiperspeed++;
                if (wiperswitchsound != -1)
                {
                    SoundManager.Play(wiperswitchsound, 1.0, 1.0, false);
                }
            }
            else if (request == 1 && wiperspeed > 0)
            {
                wiperspeed--;
                if (wiperswitchsound != -1)
                {
                    SoundManager.Play(wiperswitchsound, 1.0, 1.0, false);
                }
            }
        }
    }
}
