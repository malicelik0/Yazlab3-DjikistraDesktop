using System;

namespace TSP.Core
{
 
    [Obsolete("MutationHelper is deprecated, please use DijikstraAlgoritmasi.Mutation instead.")]
    public static class MutationHelper
    {
        
        public static void Mutation (this Chromosome child, System.Random rand)
        {
            
            int bit0 = rand.Next(0, child.Genome.Length - 1);
            int bit1;
            do
            {
                bit1 = rand.Next(0, child.Genome.Length - 1);
            }
          
            while (bit1 == bit0); 
            int buffer = child.Genome[bit0];
       
            child.Genome[bit0] = child.Genome[bit1];
            
            child.Genome[bit1] = buffer;
      
        }
    }
}
