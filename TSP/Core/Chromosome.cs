using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TSP.Core
{
    public class Chromosome
    {
       
        public static List<Point> CitiesPosition { get; set; } = new List<Point>();

      
        public int[] Genome { get; set; }

       
        public int Length { get; set; }

     
        public double Fitness { get; private set; }

      
        public Chromosome(int len) // for define array length
        {
            Length = len;
            Genome = Enumerable.Repeat(-1, Length).ToArray();
            Fitness = double.MaxValue;
        }

 
        public void Evaluate()
        {
            double fit = 0;
            for (var i = 0; i < Length - 2; i++)
                fit += CalcDistance(CitiesPosition[Genome[i]], CitiesPosition[Genome[i + 1]]);

        
            Fitness = fit;
        }

        public Chromosome Randomize()
        {
            var nums = Enumerable.Range(0, Length).ToList(); // Length:8 => [1,2,3,4,5,6,7,8]
            for (var g = 0; g < Length; g++)
            {
                var rand = nums.OrderBy(x => Guid.NewGuid()).First();  
                Genome[g] = rand;
                nums.Remove(rand);
            }

            Evaluate();
            return this;
        }

        private double CalcDistance(Point p1, Point p2)
        {
            var xDiff = p2.X - p1.X;
            var yDiff = p2.Y - p1.Y;

            return Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
        }
    }
}
