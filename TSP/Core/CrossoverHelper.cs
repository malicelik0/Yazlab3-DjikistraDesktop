using System;
using System.Collections;

namespace TSP.Core
{
    [Obsolete("CrossoverHelper is deprecated, please use DijikstraAlgoritmasi.Crossover instead.")]
    public static class CrossoverHelper
    {

        public static Chromosome Crossover(this Chromosome dad, Chromosome mum, System.Random rand)
        {
            bool write = false;

            ArrayList duplicate = new ArrayList();

            Chromosome offspring = new Chromosome(dad.Genome.Length);
            
          
            int indexDad = rand.Next(0, dad.Genome.Length - 1);
            int indexMum = mum.Genome.IndexOf(dad.Genome[indexDad]);
       
            push_info(offspring.Genome, dad.Genome[indexDad], "Center", duplicate, out write);
          
            indexDad--;  
            indexMum++;  
             
            int childLenght = offspring.Genome.Length - 1;  
            while (childLenght > 0) 
            {
          
                if (indexDad < 0) indexDad = dad.Genome.Length - 1;
                if (indexMum >= mum.Genome.Length) indexMum = 0;

                write = false;
                offspring.Genome.push_info(dad.Genome[indexDad], "Left", duplicate, out write);
                if (write) childLenght--;
                write = false;
                offspring.Genome.push_info(mum.Genome[indexMum], "Right", duplicate, out write);
                if (write) childLenght--;
              
                indexDad--;
                indexMum++;
            }

            return offspring;
        }
 
        private static int IndexOf(this int[] array, int info)
        {
            for (int f = 0; f < array.Length; f++)
                if (array[f] == info)
                    return f;
            return -1; 
        }
 
        private static void push_info(this int[] placeArray, int info, string locCourse, ArrayList duplicate, out bool write)
        {
            write = false;
           
            if (duplicate.Contains(info)) return; 
            else duplicate.Add(info);

            write = true;
        
            switch (locCourse)
            {
                case "Center":
                    {
                        var index = Convert.ToInt32(Math.Floor(Convert.ToDouble(placeArray.Length / 2)));
                        placeArray[index] = info;
                        write = true;
                    }
                    return;
                case "Left":
                    {
                        for (int l = 0; l < placeArray.Length; l++)  
                            if (placeArray[l] != -1)  
                            {                                          
                                if ((l - 1) < 0)   
                                {
                                 
                                    for (int s = placeArray.Length - 1; s > 0; s--)
                                        placeArray[s] = placeArray[s - 1];
                                   
                                    placeArray[0] = info;
                                    return;
                                }
                                else
                                {
                                    placeArray[l - 1] = info;
                                    return;
                                }
                            }
                     
                        placeArray[0] = info;
                    }
                    return;
                case "Right":
                    {
                        for (int l = placeArray.Length - 1; l >= 0; l--)       
                            if (placeArray[l] != -1) 
                            {                     
                                if ((l + 1) >= placeArray.Length)   
                                {
                                   
                                    for (int s = 0; s < placeArray.Length - 1; s++) 
                                        placeArray[s] = placeArray[s + 1];
                          
                                    placeArray[placeArray.Length - 1] = info;
                                    return;
                                }
                                else
                                {
                                    placeArray[l + 1] = info;
                                    return;
                                }
                            }
                      
                        placeArray[placeArray.Length - 1] = info;
                    }
                    return;
                default: return;
            }
        }
    }
}