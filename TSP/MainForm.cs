using GMap.NET;
using GMap.NET.WindowsForms;
using Microsoft.VisualBasic.PowerPacks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TSP.Core;
using ZedGraph;
using ThreadState = System.Threading.ThreadState;

namespace TSP
{
    public partial class MainForm : Form
    {
     
        [STAThread]
        static void Main()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Application.EnableVisualStyles();
            Application.Run(new MainForm());
        }

        #region Define Varibale
        PointPairList[] _pPlistTfg;
        PointPairList[] _pPlistTgg;
        PointPairList[] _pPlistGfg;
 

        CancellationTokenSource _tokenSource;
        int _startedTick = 0;

        public int CountCpuCore { get; set; } = 1;
         
        public int PopulationNumber { get; set; } = 500;
         
        int _nKeep = 0;
         
        double[] _pn;

        private ShapeContainer ShapeContainerAllCityShape { get; set; }
        private List<LineShape> LineShapeWay { get; set; } = new List<LineShape>();
        public List<OvalShape> OvalShapeCity { get; set; } = new List<OvalShape>();
         
        public int CounterCity { get; private set; }
        public DijikstraAlgoritmasi djikistra { get; private set; }

        // yeni işlem başlatmaya yarar
        Thread _runTime;

        #endregion

        GMapOverlay pharmOverlay = new GMapOverlay("Pharm");
        GMapOverlay routes = new GMapOverlay("routes");

        Dictionary<string, GMapMarker> listMarkers = new Dictionary<string, GMapMarker>();
        List<GMapRoute> mapRoutes = new List<GMapRoute>();

        Dictionary<string, PointLatLng> locations = new Dictionary<string, PointLatLng>();

        List<ListViewItem> listViewItems = new List<ListViewItem>();

        GMapMarker currentMarker;
        GMapMarker marker1;
        GMapRoute route;

        public MainForm()
        {
            InitializeComponent();
           
         
            ShapeContainerAllCityShape = new ShapeContainer
            {
                Location = new Point(0, 0),
                Margin = new Padding(0),
                Size = new Size(Width, Height),
                TabIndex = 0,
                TabStop = false
            };

            Controls.Add(ShapeContainerAllCityShape);
            
            _pPlistTfg = new PointPairList[2];  
            _pPlistTfg[0] = new PointPairList(); 
            _pPlistTfg[1] = new PointPairList();  

            _pPlistTgg = new PointPairList[2];
            _pPlistTgg[0] = new PointPairList(); 

            _pPlistTgg[1] = new PointPairList(); 

            _pPlistGfg = new PointPairList[2]; 
            _pPlistGfg[0] = new PointPairList();
            _pPlistGfg[1] = new PointPairList();
        }

        #region Thread Invoked
        public static void UiInvoke(Control uiControl, Action action)
        {
            if (!uiControl.IsDisposed)
            {
                if (uiControl.InvokeRequired)
                {
                    uiControl.BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
        }

 
        delegate void SetValueCallback(int v);
        private void SetValue(int v)
        {
            
            try
            {
                if (statusStrip1.InvokeRequired)
                {
                    var d = new SetValueCallback(SetValue);
                    Invoke(d, new object[] { v });
                }
                else
                {
                    toolStripProgressBar1.Value = v;
                }
            }
            catch { }
        }

      
        delegate void SetMaxValueCallback(int v);
        private void SetMaxValue(int v)
        {
            
            try
            {
                if (statusStrip1.InvokeRequired)
                {
                    var d = new SetMaxValueCallback(SetMaxValue);
                    Invoke(d, new object[] { v });
                }
                else
                {
                    toolStripProgressBar1.Maximum = v;
                }
            }
            catch { }
        }

   
        private void SetLenghtText(string v)
        {
            try
            {
                try
                {
                    UiInvoke(lblLenght, delegate ()
                    {
                        lblLenght.Text = v;
                    });
                }
                catch { }
            }
            catch { }
        }
         delegate void AddShapeCallback(LineShape l);
        private void AddLineShape(LineShape l)
        {
            try
            {
                if (ShapeContainerAllCityShape.InvokeRequired)
                {
                    var d = new AddShapeCallback(AddLineShape);
                    Invoke(d, new object[] { l });
                }
                else
                {
                    ShapeContainerAllCityShape.Shapes.Add(l);
                }
            }
            catch
            {
                
            }
        }
       
        delegate void RemoveShapeCallback(LineShape l);
        private void RemoveLineShape(LineShape l)
        {
            try
            {
                if (ShapeContainerAllCityShape.InvokeRequired)
                {
                    var d = new RemoveShapeCallback(RemoveLineShape);
                    Invoke(d, new object[] { l });
                }
                else
                {
                    ShapeContainerAllCityShape.Shapes.Remove(l);
                }
            }
            catch { }
        }
   
        delegate void SetPointCallback(int l, Point p0, Point p1);
        private void SetPoint(int l, Point p0, Point p1)
        {
            try
            {
                LineShapeWay[l].X1 = p0.X + 10;
                LineShapeWay[l].X2 = p1.X + 10;
                LineShapeWay[l].Y1 = p0.Y + 10;
                LineShapeWay[l].Y2 = p1.Y + 10;
            }
            catch { }
        }
        // ------------------------------------------------------------
        private void SetNumPopEnable(bool en)
        {
            try
            {
                try
                {
                    UiInvoke(numPopulation, delegate ()
                    {
                        numPopulation.Enabled = en;
                    });
                }
                catch { }
            }
            catch { }
        }
        #endregion

     
        public void Ga()
        {
            var rand = new System.Random();
            var eliteFitness = double.MaxValue;
 
            SetCitiesPosition(OvalShapeCity);
            
            CountCpuCore = CalcCountOfCpu();  
            _tokenSource = new CancellationTokenSource();
            //
            // set Start TickTime
            _startedTick = Environment.TickCount;

            if (pGAToolStripMenuItem.Checked)  
            {
                _pPlistTfg[1].Clear();
                _pPlistGfg[1].Clear();
                _pPlistTgg[1].Clear();
            }
            else  
            {
                _pPlistTfg[0].Clear();
                _pPlistGfg[0].Clear();
                _pPlistTgg[0].Clear();
            }
          //burada newlenip kullanılıyor
            djikistra = new DijikstraAlgoritmasi(CounterCity, PopulationNumber, 10, 50, 10000, 75);
            
            var count = 0;
            SetValue(0);
            
            if (CounterCity <= 5)
                SetMaxValue(100);
           
            //
            else if (CounterCity <= 15)
                SetMaxValue(1000);
       
            //
            else if (CounterCity <= 30)
                SetMaxValue(10000);
 
            //
            else if (CounterCity <= 40)
                SetMaxValue(51000);
          
            //
            else if (CounterCity <= 60)
                SetMaxValue(100000);
        
            //
            else
                SetMaxValue(1000000);
    
       

            do
            {
                #region Selection
                #region Bubble Sort all chromosome by fitness
                // 
                for (var i = PopulationNumber - 1; i > 0; i--)
                    for (var j = 1; j <= i; j++)
                        if (djikistra.Population[j - 1].Fitness > djikistra.Population[j].Fitness)
                        {
                            var ch = djikistra.Population[j - 1];
                            djikistra.Population[j - 1] = djikistra.Population[j];
                            djikistra.Population[j] = ch;
                        }
                //
                #endregion

                #region Elitism
                if (eliteFitness > djikistra.Population[0].Fitness)
                {
                    eliteFitness = djikistra.Population[0].Fitness;
                    SetTimeGraph(eliteFitness, count, true);

                    if (dynamicalGraphicToolStripMenuItem.Checked)  
                    {
                        RefreshTour();
                    }
                    //
                    //-----------------------------------------------------------------------------
                    SetLenghtText(djikistra.Population[0].Fitness.ToString());
                    //
                }
               
                #endregion
                x_Rate(); 
                #endregion

                #region Reproduction
               
                Rank_Trim();

                if (pGAToolStripMenuItem.Checked)  
                {
                    if (threadParallelismToolStripMenuItem.Checked)  
                    {
                        ReproduceByParallelThreads();
                    }
                    else if (taskParallelismToolStripMenuItem.Checked)  
                    {
                        ReproduceByParallelTask();
                    }
                    else if (parallelForToolStripMenuItem.Checked)  
                    {
                        PReproduction(rand);
                    }
                }
                else // Series djikistra Algorithm
                {
                    #region Series Reproduct Code
                    Reproduction(rand);
                    #endregion
                }
                #endregion

                count++;
              
                //
                SetValue(toolStripProgressBar1.Value + 1);
           
            }
            while (count < toolStripProgressBar1.Maximum && Isotropy_Evaluatuon());

          
            SetValue(toolStripProgressBar1.Maximum);
          
            SetNumPopEnable(true);
        
            Stop();
        }

        #region Generation Tools
 
        private void x_Rate()
        {
       
            double sumFitness = 0;
            for (var i = 0; i < PopulationNumber; i++)
                sumFitness += djikistra.Population[i].Fitness;
          
            var aveFitness = sumFitness / PopulationNumber;  
            _nKeep = 0; 
            for (var i = 0; i < PopulationNumber; i++)
                if (aveFitness >= djikistra.Population[i].Fitness)
                {
                    _nKeep++; 
                }
            if (_nKeep <= 0) _nKeep = 2;
        }
         
        private void Rank_Trim()
        {
             
            //
            _pn = new double[_nKeep]; // Create chromosome possibility Array Cell as N_keep
            double sum = ((_nKeep * (_nKeep + 1)) / 2); // (∑ No.chromosome) == (n(n+1) / 2)
            _pn[0] = _nKeep / sum; // Father (Best - Elite) chromosome Possibility
            for (var i = 1; i < _nKeep; i++)
            {
                // Example: if ( Pn[Elite] = 0.4  &  Pn[Elite +1] = 0.2  &  Pn[Elite +2]  = 0.1 )
                // Then Own:          0 <= R <= 0.4 ===> Select chromosome[Elite]
                //                  0.4 <  R <= 0.6 ===> Select chromosome[Elite +1] 
                //                  0.6 <  R <= 0.7 ===> Select chromosome[Elite +2]
                // etc ... 
                _pn[i] = ((_nKeep - i) / sum) + _pn[i - 1];
            }
        }

        // Return Father and Mather chromosome with Probability of chromosome fitness
        private Chromosome Rank(System.Random rand)
        {
            var r = rand.NextDouble();
            for (var i = 0; i < _nKeep; i++)
            {
                // Example: if ( Pn[Elite] = 0.6  &  Pn[Elite+1] = 0.3  &  Pn[Elite+2]  = 0.1 )
                // Then Own:          0 <= R <= 0.6  ===> Select chromosome[Elite]
                //                  0.6 <  R <= 0.9  ===> Select chromosome[Elite +1] 
                //                  0.9 <  R <= 1    ===> Select chromosome[Elite +2]
                // 
                if (r <= _pn[i]) return djikistra.Population[i];
            }
            return djikistra.Population[0]; // if don't run Modality of 'for' then return Elite chromosome 
        }

        // Check the isotropy All REMNANT chromosome (N_keep)
        public bool Isotropy_Evaluatuon()
        {
            // Isotropy percent is 50% of All chromosome Fitness
            var perIso = Convert.ToInt32(Math.Truncate(Convert.ToDouble(50 * _nKeep / 100)));
            var counterIsotropy = 0;
            var bestFitness = djikistra.Population[0].Fitness;
            //
            // i start at 1 because DNA_Array[0] is self BestFitness
            for (var i = 1; i < _nKeep; i++)
                if (bestFitness >= djikistra.Population[i].Fitness) counterIsotropy++;

            // G.A Algorithm did isotropy and app Stopped
            if (counterIsotropy >= perIso) return false;
            else return true; // G.A Algorithm didn't isotropy and app will continued
        }

        private void ReproduceByParallelThreads()
        {
            #region Parallel Reproduct Code
            var th = new Thread[CountCpuCore];

            // Create a semaphore that can satisfy up to three
            // concurrent requests. Use an initial count of zero,
            // so that the entire semaphore count is initially
            // owned by the main program thread.
            //
            var sem = new Semaphore(CountCpuCore, CountCpuCore);
            var isAlive = new bool[CountCpuCore];
            var isCompleted = new bool[CountCpuCore];

            var length = (PopulationNumber - _nKeep) / CountCpuCore;
            var divideReminder = (PopulationNumber - _nKeep) % CountCpuCore;

            for (var proc = 0; proc < th.Length; proc++)
            {
                var tt = new ThreadToken(proc,
                    length + ((proc == CountCpuCore - 1) ? divideReminder : 0),
                    _nKeep + (proc * length));

                th[proc] = new Thread((x) =>
                {
                    // Entered
                    sem.WaitOne();
                    isAlive[((ThreadToken)x).No] = true;

                    // work ...
                    PReproduction(((ThreadToken)x).StartIndex, ((ThreadToken)x).Length, ((ThreadToken)x).Rand);

                    // We have finished our job, so release the semaphore
                    isCompleted[((ThreadToken)x).No] = true;
                    sem.Release();
                });
                SetThreadPriority(th[proc]);
                th[proc].Start(tt);
            }

        startloop:
            foreach (var alive in isAlive) // wait parent starter for start all children.
                if (!alive)
                    goto startloop;

                endLoop:
            sem.WaitOne();
            foreach (var complete in isCompleted) // wait parent to interrupt for finishes all of children jobs.
                if (!complete)
                    goto endLoop;

            // Continue Parent Work
            sem.Close();
            #endregion
        }
        private void ReproduceByParallelTask()
        {
            #region Parallel Reproduct Code
            var tasks = new Task[CountCpuCore];

            var length = (PopulationNumber - _nKeep) / CountCpuCore;
            var divideReminder = (PopulationNumber - _nKeep) % CountCpuCore;

            for (var proc = 0; proc < tasks.Length; proc++)
            {
                var tt = new ThreadToken(proc,
                    length + ((proc == CountCpuCore - 1) ? divideReminder : 0),
                    _nKeep + (proc * length));

                tasks[proc] = Task.Factory.StartNew(x =>
                {
                    // work ...
                    PReproduction(((ThreadToken)x).StartIndex, ((ThreadToken)x).Length, ((ThreadToken)x).Rand);

                }, tt, _tokenSource.Token);// TaskCreationOptions.AttachedToParent);
            }

            // When user code that is running in a task creates a task with the AttachedToParent option, 
            // the new task is known as a child task of the originating task, 
            // which is known as the parent task. 
            // You can use the AttachedToParent option to express structured task parallelism,
            // because the parent task implicitly waits for all child tasks to complete. 
            // The following example shows a task that creates one child task:
            Task.WaitAll(tasks);

            // or

            //Block until all tasks complete.
            //Parent.Wait(); // when all task are into a parent task
            #endregion
        }
   
        public void Reproduction(System.Random rand) 
        {
            for (var i = _nKeep; i < PopulationNumber; i++)
            { 
                Chromosome rankFather, rankMather;
                 
                do
                {
                    rankFather = Rank(rand);
                    rankMather = Rank(rand);
                }
                while (rankFather == rankMather);
             
                var child = rankMather.Crossover(rankFather, new System.Random());
                
                child.Mutation(new System.Random());
              
                child.Evaluate();

                Interlocked.Exchange(ref djikistra.Population[i], child);  
            }
        }
      
        public void PReproduction(int startIndex, int length, System.Random rand) // Parallel 
        {
            for (var i = startIndex; i < (startIndex + length) && i < PopulationNumber; i++)
            {
             
                Chromosome rankFather, rankMather;

                
                do
                {
                    rankFather = Rank(rand);
                    rankMather = Rank(rand);
                }
                while (rankFather == rankMather);
                //
                // CrossoverHelper
                var child = rankMather.Crossover(rankFather, new System.Random());
                //
                //  run MutationHelper
                //
                child.Mutation(new System.Random());
                //
                // calculate children chromosome fitness
                //
                child.Evaluate();

                Interlocked.Exchange(ref djikistra.Population[i], child); // atomic operation between multiple Thread shared
            }
        }

     
        public void PReproduction(System.Random rand) // Parallel.For 
        {
            Parallel.For(_nKeep, PopulationNumber,
                        new ParallelOptions() { MaxDegreeOfParallelism = CountCpuCore, CancellationToken = _tokenSource.Token },
                        (i, loopState) =>
                        {
                         
                            Chromosome rankFather, rankMather;
                            do
                            {
                                Monitor.Enter(rand);
                                rankFather = Rank(rand);
                                rankMather = Rank(rand);
                                Monitor.Exit(rand);
                            }
                            while (rankFather == rankMather);
                            //
                            // CrossoverHelper
                            var child = rankMather.Crossover(rankFather, new System.Random());
                            //
                            //  run MutationHelper
                            //
                            child.Mutation(new System.Random());
                            //
                            // calculate children chromosome fitness
                            //
                            child.Evaluate();

                            Interlocked.Exchange(ref djikistra.Population[i], child); // atomic operation between multiple Thread shared

                            if (_tokenSource.IsCancellationRequested || _tokenSource.Token.IsCancellationRequested)
                            {
                                loopState.Stop();
                                loopState.Break();
                                return;
                            }
                        });
        }

        #endregion

        private void SetCitiesPosition(List<OvalShape> ovalShapeCity)
        {
            Chromosome.CitiesPosition.Clear();
            foreach (var city in ovalShapeCity)
                Chromosome.CitiesPosition.Add(city.Location);
        }

        private void Stop()
        {
            if (_runTime != null)
            {
                if (_runTime.IsAlive)
                {
                    SetNumPopEnable(true); 
                    UiInvoke(btnStartStop, delegate ()
                    {
                        btnStartStop.Checked = false;
                    });
                    UiInvoke(btnPauseResume, delegate ()
                    {
                        btnPauseResume.Checked = false;
                    });
                    try
                    {
                        if (pGAToolStripMenuItem.Checked)
                        {
                            _tokenSource.Cancel();
                        }
                        _runTime.Abort();
                    }
                    catch { }
                    RefreshTour();
                }
            }
        }

        private int CalcCountOfCpu()
        {
            var numCore = 0;

            #region Find number of Active CPU or CPU core's for this Programs

            var affinityDec = Process.GetCurrentProcess().ProcessorAffinity.ToInt64();
            var affinityBin = Convert.ToString(affinityDec, 2); // toBase 2
            foreach (var anyOne in affinityBin.ToCharArray())
                if (anyOne == '1') numCore++;

            #endregion

            //if (numCore > 2) return --numCore;
            return numCore;
        }

        private void SetThreadPriority(Thread th)
        {
            if (th != null)
            {
                if (th.ThreadState != ThreadState.Aborted &&
                   th.ThreadState != ThreadState.AbortRequested &&
                   th.ThreadState != ThreadState.Stopped &&
                   th.ThreadState != ThreadState.StopRequested)
                {
                    switch (Process.GetCurrentProcess().PriorityClass)
                    {
                        case ProcessPriorityClass.AboveNormal:
                            th.Priority = ThreadPriority.AboveNormal;
                            break;
                        case ProcessPriorityClass.BelowNormal:
                            th.Priority = ThreadPriority.BelowNormal;
                            break;
                        case ProcessPriorityClass.High:
                            th.Priority = ThreadPriority.Highest;
                            break;
                        case ProcessPriorityClass.Idle:
                            th.Priority = ThreadPriority.Lowest;
                            break;
                        case ProcessPriorityClass.Normal:
                            th.Priority = ThreadPriority.Normal;
                            break;
                        case ProcessPriorityClass.RealTime:
                            th.Priority = ThreadPriority.Highest;
                            break;
                    }
                    //
                    // Set Thread Affinity 
                    //
                    Thread.BeginThreadAffinity();
                }
            }
        }

        private void SetTimeGraph(double eliteFitness, long generation, bool fitnessRefreshed)
        {
            var timeLenght = (Environment.TickCount - _startedTick) / 10; // Convert to MiliSecond
            if (pGAToolStripMenuItem.Checked)
            {
                if (fitnessRefreshed)
                {
                    _pPlistTfg[1].Add(timeLenght, eliteFitness);
                    _pPlistGfg[1].Add(generation, eliteFitness);
                }
                _pPlistTgg[1].Add(timeLenght, generation);
            }
            else
            {
                if (fitnessRefreshed)
                {
                    _pPlistTfg[0].Add(timeLenght, eliteFitness);
                    _pPlistGfg[0].Add(generation, eliteFitness);
                }
                _pPlistTgg[0].Add(timeLenght, generation);
            }
        }

        private void refreshDGV_CityPositions()
        {
            dgvCity.Rows.Clear();

            for (var count = 0; count < OvalShapeCity.Count; count++)
            {
                dgvCity.Rows.Add(new object[] { count + 1, OvalShapeCity[count].Location.ToString() });
            }
        }

        public void create_City(Point e) // konum ekler
        {
            CounterCity++;
            toolStripStatuslblNumCity.Text = CounterCity.ToString();
            var newCity = new OvalShape();
            // 
            // newCity
            // 
            if (CounterCity == 1)
            {
                //fotoğraf eklencek todo
                
                newCity.BackgroundImage = Properties.Resources.xx;
                newCity.BackStyle = BackStyle.Opaque;
                newCity.BorderColor = Color.White;
                newCity.BackColor = Color.Blue;
             
                newCity.Cursor = Cursors.Hand;
                newCity.Location = new Point(e.X, e.Y);
                newCity.Size = new Size(30, 30);
                newCity.Click += ovalShape_Click;
                this.Opacity = 1;
                newCity.BringToFront();
               
            }
            else
            {

                newCity.BackgroundImage = Properties.Resources.black_drone_png_clipart_26;
                newCity.BackStyle = BackStyle.Opaque;
                newCity.BorderColor = Color.Black;
                newCity.BackColor = Color.Red;
                newCity.Cursor = Cursors.Hand;
                newCity.Location = new Point(e.X, e.Y);
                newCity.Size = new Size(20, 20);
                newCity.Click += ovalShape_Click;
                newCity.BringToFront();
            }
            
            OvalShapeCity.Add(newCity);
            ShapeContainerAllCityShape.Shapes.Add(newCity);

         
           
        }

        private void RefreshTour()
        {
            try
            {
                Point point1, point0;
                for (var c = 1; c <= CounterCity; c++)
                    try
                    {
 
                        RemoveLineShape(LineShapeWay[c]);
                        //
                    }
                    catch { break; }

                for (var c = 1; c < CounterCity; c++)
                { 
                    point1 = OvalShapeCity[djikistra.Population[0].Genome[c]].Location;
                    point0 = OvalShapeCity[djikistra.Population[0].Genome[c - 1]].Location;

                    try
                    {
                        var d = new SetPointCallback(SetPoint);
                        BeginInvoke(d, new object[] { c, point0, point1 });
                    }
                    catch { }

                     AddLineShape(LineShapeWay[c]);
                    //
                }
                 point1 = OvalShapeCity[djikistra.Population[0].Genome[CounterCity - 1]].Location;
                point0 = OvalShapeCity[djikistra.Population[0].Genome[0]].Location;

                try
                {
                    var d2 = new SetPointCallback(SetPoint);
                    BeginInvoke(d2, new object[] { 0, point0, point1 });
                }
                catch { }

                AddLineShape(LineShapeWay[0]);
            }
            catch { }
        }

        private void ovalShape_Click(object sender, EventArgs e)
        {
            CounterCity--;
            OvalShapeCity.Remove((OvalShape)sender);
            ShapeContainerAllCityShape.Shapes.Remove(((OvalShape)sender)); // Seçilen şekli silme
            toolStripStatuslblNumCity.Text = CounterCity.ToString();
            //
            // Refresh şekiller
            refreshDGV_CityPositions();
        }

        //mouse hareket edince sol altta lat long yazdırılıyor
        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            toolStripStatuslblLocate.Text = "Lat " + e.X.ToString() + " ,  Long " + e.Y.ToString();
        }

        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            var mPosition = new Point(e.X - 10, e.Y - 10);
            if (mPosition.X > 1 && mPosition.X < Width - 300 && mPosition.Y > 65 && mPosition.Y < Height - 85)
            {
                Stop();
                foreach (var anyLine in LineShapeWay)
                    ShapeContainerAllCityShape.Shapes.Remove(anyLine);
                LineShapeWay.Clear();
                create_City(mPosition);
                //
                // Refresh City Positions List
                refreshDGV_CityPositions();
            }
        }

        private void numPopulation_ValueChanged(object sender, EventArgs e)
        {
            PopulationNumber = (int)numPopulation.Value;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stop();
            //
            // Remove Old City and road
            //
            foreach (var city in OvalShapeCity)
                ShapeContainerAllCityShape.Shapes.Remove(city);
            foreach (var anyLine in LineShapeWay)
                ShapeContainerAllCityShape.Shapes.Remove(anyLine);
            OvalShapeCity.Clear();
            CounterCity = 0;
            //
            // Refresh City Position List
            //
            refreshDGV_CityPositions();
           
            toolStripProgressBar1.ProgressBar.Value = 0;
            //lblGeneration.Text = "0000";
            lblLenght.Text = "0000";
            toolStripStatuslblNumCity.Text = "0";
        }

 
       
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stop();
            for (var th = 0; th < Process.GetCurrentProcess().Threads.Count; th++)
                Process.GetCurrentProcess().Threads[th].Dispose();
            Application.Exit();
        }

        private void dynamicalGraphicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dynamicalGraphicToolStripMenuItem.Checked = !dynamicalGraphicToolStripMenuItem.Checked;

            if (dynamicalGraphicToolStripMenuItem.Checked) RefreshTour();

            toolsToolStripMenuItem.ShowDropDown();
        }

       

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var about = new FormAbout();
            about.ShowDialog();
        }

        private void btnStartStop_CheckedChanged(object sender, EventArgs e)
        {
         
            if (btnStartStop.Checked)
            {
                btnPauseResume.Enabled = true;
                btnStartStop.Text = "Durdur";
                #region Start
                if (OvalShapeCity.Count <= 1)
                {
                    btnStartStop.Checked = false;
                    MessageBox.Show("Lütfen konum seçiniz!", "Konum Hatası",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return;
                }
                //
                #region Djikistra Algoritma başlangıç
              
                numPopulation.Enabled = false;
                // 
                // Mavi çizgi çekmek
                // 
                foreach (var anyLine in LineShapeWay)
                    ShapeContainerAllCityShape.Shapes.Remove(anyLine);
                LineShapeWay.Clear();

                foreach (var shape in ShapeContainerAllCityShape.Shapes)
                {
                    if (shape.GetType() != typeof(OvalShape) && shape is Shape s)
                    {
                        ShapeContainerAllCityShape.Shapes.Remove(s);
                    }
                }

                ShapeContainerAllCityShape.Refresh();

                for (var c = 0; c < OvalShapeCity.Count; c++)
                {
                    var newLine = new LineShape
                    {
                           
                        BorderColor = Color.Blue,
                        Cursor = Cursors.Default,
                        Enabled = false
                    };
                    LineShapeWay.Add(newLine);
                }
                //
                //
                #endregion
                //
                // Solve();
                try
                {
                
                        _runTime = new Thread(Ga);
                        SetThreadPriority(_runTime);
                        _runTime.Start();
                     
                }
                catch
                {
                    _runTime = new Thread(Ga);
                    SetThreadPriority(_runTime);
                    _runTime.Start();
                }
                #endregion
            }
            else
            {
                if (btnPauseResume.Checked)
                {
                    btnPauseResume.Checked = false;
                }
                btnStartStop.Text = @"&Başlat";
                Stop();
            }
        }

        private void btnPauseResume_CheckedChanged(object sender, EventArgs e)
        {
            if (btnPauseResume.Checked)
            {
                btnPauseResume.Text = @"&Devam Ettir";
                try
                {
                    if (_runTime.IsAlive)
                        _runTime.Suspend();
                }
                catch { }
            }
            else
            {
                btnPauseResume.Text = @"&Durdur";
                try { if (_runTime.ThreadState == ThreadState.Suspended) _runTime.Resume(); }
                catch { }

                foreach (var anyLine in LineShapeWay)
                    ShapeContainerAllCityShape.Shapes.Remove(anyLine);
                foreach (var anyLine in LineShapeWay)
                    ShapeContainerAllCityShape.Shapes.Add(anyLine);
            }
        }

        private void RealtimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((ToolStripMenuItem)sender).Checked)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
                HighToolStripMenuItem.Checked = false;
                AboveNormalToolStripMenuItem.Checked = false;
                NormalToolStripMenuItem.Checked = false;
                BelowNormalToolStripMenuItem.Checked = false;
                LowToolStripMenuItem.Checked = false;
                ProcessPriorityToolStripMenuItem.ShowDropDown();
                SetPriorityToolStripMenuItem.ShowDropDown();
            }
            else
            {
                ((ToolStripMenuItem)sender).Checked = true;
                ProcessPriorityToolStripMenuItem.ShowDropDown();
                SetPriorityToolStripMenuItem.ShowDropDown();
            }
            SetThreadPriority(_runTime);
        }

        private void HighToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((ToolStripMenuItem)sender).Checked)
            {
                RealtimeToolStripMenuItem.Checked = false;
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                AboveNormalToolStripMenuItem.Checked = false;
                NormalToolStripMenuItem.Checked = false;
                BelowNormalToolStripMenuItem.Checked = false;
                LowToolStripMenuItem.Checked = false;
                ProcessPriorityToolStripMenuItem.ShowDropDown();
                SetPriorityToolStripMenuItem.ShowDropDown();
            }
            else
            {
                ((ToolStripMenuItem)sender).Checked = true;
                ProcessPriorityToolStripMenuItem.ShowDropDown();
                SetPriorityToolStripMenuItem.ShowDropDown();
            }
            SetThreadPriority(_runTime);
        }

        private void AboveNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((ToolStripMenuItem)sender).Checked)
            {
                RealtimeToolStripMenuItem.Checked = false;
                HighToolStripMenuItem.Checked = false;
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
                NormalToolStripMenuItem.Checked = false;
                BelowNormalToolStripMenuItem.Checked = false;
                LowToolStripMenuItem.Checked = false;
                ProcessPriorityToolStripMenuItem.ShowDropDown();
                SetPriorityToolStripMenuItem.ShowDropDown();
            }
            else
            {
                ((ToolStripMenuItem)sender).Checked = true;
                ProcessPriorityToolStripMenuItem.ShowDropDown();
                SetPriorityToolStripMenuItem.ShowDropDown();
            }
            SetThreadPriority(_runTime);
        }

        private void NormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((ToolStripMenuItem)sender).Checked)
            {
                RealtimeToolStripMenuItem.Checked = false;
                HighToolStripMenuItem.Checked = false;
                AboveNormalToolStripMenuItem.Checked = false;
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                BelowNormalToolStripMenuItem.Checked = false;
                LowToolStripMenuItem.Checked = false;
                ProcessPriorityToolStripMenuItem.ShowDropDown();
                SetPriorityToolStripMenuItem.ShowDropDown();
            }
            else
            {
                ((ToolStripMenuItem)sender).Checked = true;
                ProcessPriorityToolStripMenuItem.ShowDropDown();
                SetPriorityToolStripMenuItem.ShowDropDown();
            }
            SetThreadPriority(_runTime);
        }

        private void BelowNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((ToolStripMenuItem)sender).Checked)
            {
                RealtimeToolStripMenuItem.Checked = false;
                HighToolStripMenuItem.Checked = false;
                AboveNormalToolStripMenuItem.Checked = false;
                NormalToolStripMenuItem.Checked = false;
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
                LowToolStripMenuItem.Checked = false;
                ProcessPriorityToolStripMenuItem.ShowDropDown();
                SetPriorityToolStripMenuItem.ShowDropDown();
            }
            else
            {
                ((ToolStripMenuItem)sender).Checked = true;
                ProcessPriorityToolStripMenuItem.ShowDropDown();
                SetPriorityToolStripMenuItem.ShowDropDown();
            }
            SetThreadPriority(_runTime);
        }

        private void LowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((ToolStripMenuItem)sender).Checked)
            {
                RealtimeToolStripMenuItem.Checked = false;
                HighToolStripMenuItem.Checked = false;
                AboveNormalToolStripMenuItem.Checked = false;
                NormalToolStripMenuItem.Checked = false;
                BelowNormalToolStripMenuItem.Checked = false;
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
                ProcessPriorityToolStripMenuItem.ShowDropDown();
                SetPriorityToolStripMenuItem.ShowDropDown();
            }
            else
            {
                ((ToolStripMenuItem)sender).Checked = true;
                ProcessPriorityToolStripMenuItem.ShowDropDown();
                SetPriorityToolStripMenuItem.ShowDropDown();
            }
            SetThreadPriority(_runTime);
        }

        private void SetAffinityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            CountCpuCore = CalcCountOfCpu();
        }

        private void pGAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if checked then  Parallel djikistra Algorithm Enable
            pGAToolStripMenuItem.Checked = !pGAToolStripMenuItem.Checked;
            if (pGAToolStripMenuItem.Checked) taskParallelismToolStripMenuItem.Checked = true;
            else
            {
                taskParallelismToolStripMenuItem.Checked = false;
                threadParallelismToolStripMenuItem.Checked = false;
                parallelForToolStripMenuItem.Checked = false;
            }
            // show Panel
            ProcessPriorityToolStripMenuItem.ShowDropDown();
            pGAToolStripMenuItem.ShowDropDown();
        }

        private void taskParallelismToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // First change self check to unOlder self check's
            taskParallelismToolStripMenuItem.Checked = !taskParallelismToolStripMenuItem.Checked;

            // Then check PGA by self
            pGAToolStripMenuItem.Checked = taskParallelismToolStripMenuItem.Checked;

            // and check other by !self
            threadParallelismToolStripMenuItem.Checked = false;
            parallelForToolStripMenuItem.Checked = false;

            // show Panel
            ProcessPriorityToolStripMenuItem.ShowDropDown();
            pGAToolStripMenuItem.ShowDropDown();
        }

        private void threadParallelismToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // First change self check to unOlder self check's
            threadParallelismToolStripMenuItem.Checked = !threadParallelismToolStripMenuItem.Checked;

            // Then check PGA by self
            pGAToolStripMenuItem.Checked = threadParallelismToolStripMenuItem.Checked;

            // and check other by !self
            taskParallelismToolStripMenuItem.Checked = false;
            parallelForToolStripMenuItem.Checked = false;

            // show Panel
            ProcessPriorityToolStripMenuItem.ShowDropDown();
            pGAToolStripMenuItem.ShowDropDown();
        }

        private void parallelForToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // First change self check to unOlder self check's
            parallelForToolStripMenuItem.Checked = !parallelForToolStripMenuItem.Checked;

            // Then check PGA by self
            pGAToolStripMenuItem.Checked = parallelForToolStripMenuItem.Checked;

            // and check other by !self
            taskParallelismToolStripMenuItem.Checked = false;
            threadParallelismToolStripMenuItem.Checked = false;

            // show Panel
            ProcessPriorityToolStripMenuItem.ShowDropDown();
            pGAToolStripMenuItem.ShowDropDown();
        }
        public void addLocations()
        {
            //Add location
            locations.Add("ALBERTO PHARMACY # 1", new PointLatLng(49.2619707, -123.069488));
            locations.Add("ALBERTO PHARMACY NO. 2", new PointLatLng(49.2607233, -123.0695212));
            locations.Add("BENTALL PHARMACY", new PointLatLng(49.2864943, -123.12143630000003));
            locations.Add("BIOPRO BIOLOGICS PHARMACY", new PointLatLng(49.2635473, -123.12293629999999));
            locations.Add("BOND STREET PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            locations.Add("BOOMER DRUGS", new PointLatLng(49.208856, -123.140283));
            locations.Add("BROADWAY PHARMASAVE #73", new PointLatLng(49.264082, -123.151204));
            locations.Add("CAMBIE PHARMASAVE", new PointLatLng(49.249580, -123.115192));
            locations.Add("CANPHARM DRUGS", new PointLatLng(49.231110, -123.065809));
            locations.Add("CAREVILLE PHARMACY UBC", new PointLatLng(49.254691, -123.235713));
            locations.Add("CLOUD IPHARMACY INC.", new PointLatLng(49.239514, -123.065542));
            locations.Add("COAL HARBOUR PHARMACY", new PointLatLng(49.287595, -123.124223));
            locations.Add("CONTINENTAL PHARMACY #2", new PointLatLng(49.236665, -123.065088));
            locations.Add("CORNING DRUGS #2", new PointLatLng(49.278414, -123.098542));

            //locations.Add("CORNING DRUGS LTD.", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("COSTCO PHARMACY # 552", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("DAVIE PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("DOWNTOWN CLINIC PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("DTES CONNECTIONS PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("DUNDAS REMEDY'S RX PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("EAST END PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("EASTSIDE PHARMACY LTD.", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("EVERWELL PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("FINLANDIA NATURAL PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("FRASER NEIGHBOURHOOD PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("FRASER OUTREACH PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("FRASER PHARMACHOICE", new PointLatLng(49.2088562, -123.14028289999999));

            //locations.Add("FRASER PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("GARLANE PHARMACY #1", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("GARLANE PHARMACY #2", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("GARLANE PRESCRIPTIONS", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("HARVARD PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("HEALTHSIDE PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("JEFF'S PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("JERICHO PHARMACY & HEALTH FOOD STORE", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("KERRISDALE MEDICINE CENTRE PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("KINGSWAY PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("KRIPPS HEALTHCARE RX", new PointLatLng(49.2088562, -123.14028289999999));

            //locations.Add("LANCASTER MEDICAL SUPPL. & PRESC. #1", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("LAUREL PRESCRIPTIONS", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("LG PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("LITTLE MOUNTAIN PHARMACY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("LOBLAW PHARMACY #1517", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("LOBLAW PHARMACY #1520", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("LOBLAW PHARMACY #4617", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("LOBLAW PHARMACY #4979", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("LONDON DRUGS # 2", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("LONDON DRUGS # 4 - BROADWAY", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("LONDON DRUGS # 7 - HASTINGS", new PointLatLng(49.2088562, -123.14028289999999));
            //locations.Add("LONDON DRUGS #10", new PointLatLng(49.2088562, -123.14028289999999));
        }
        private void map_MouseClick(object sender, MouseEventArgs e)
        {
            var mPosition = new Point(e.X - 10, e.Y - 10);
            if (mPosition.X > 1 && mPosition.X < Width - 300 && mPosition.Y > 65 && mPosition.Y < Height - 85)
            {
                Stop();
                foreach (var anyLine in LineShapeWay)
                    ShapeContainerAllCityShape.Shapes.Remove(anyLine);
                LineShapeWay.Clear();
                create_City(mPosition);
                //
                // Refresh City Positions List
                refreshDGV_CityPositions();
            }
        }
        //private void MainMap_Load(object sender, EventArgs e)
        //{
        
        //    MainMap.MapProvider = GoogleMapProvider.Instance;
        //    GMaps.Instance.Mode = AccessMode.ServerOnly;
        //    MainMap.SetPositionByKeywords("Vancouver, Canada");
        //    MainMap.ShowCenter = false;
        //    MainMap.Overlays.Add(pharmOverlay);
        //    MainMap.Overlays.Add(routes);
        //    addLocations();

        //    //var bs = new BindingSource(locations, null);

        //    //comboBox1.DataSource = bs;
        //    //comboBox1.DisplayMember = "Key";
        //    //comboBox1.ValueMember = "Value";
        //    // feyyaz
        //    MainMap.MouseClick += new MouseEventHandler(map_MouseClick);
        //}

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void toolsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void ProcessPriorityToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void lblLenght_Click(object sender, EventArgs e)
        {

        }
    }

    public struct ThreadToken
    {
        public ThreadToken(int threadNo, int length, int startIndex)
        {
            No = threadNo;
            Length = length;
            StartIndex = startIndex;
            Rand = new System.Random();
        }
        public int No;
        public int Length;
        public int StartIndex;
        public System.Random Rand;
    };
}