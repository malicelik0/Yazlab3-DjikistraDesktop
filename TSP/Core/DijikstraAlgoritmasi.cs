using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TSP.Core
{
    public class DijikstraAlgoritmasi
    {
        public int ChromosomeLenght { get; set; }
        public int PopulationLenght { get; set; }
        public int SelectionPercent { get; set; }
        public int MutationProbability { get; set; }
        public int RegenerationLimit { get; set; }
        public int RegenerationCounter { get; set; }
        public int ConvergenceRate { get; set; }
        public Chromosome[] Population { get; set; }


        public DijikstraAlgoritmasi(int len, int? popLenght = null,
            int? selectionPercent = null, int? mutationRate = null,
            int? maxRegenerationCount = null, int? convergenceRate = null)
        {
            ChromosomeLenght = len;
            PopulationLenght = popLenght ?? 100;
            SelectionPercent = selectionPercent ?? 50;
            MutationProbability = mutationRate ?? 20;
            RegenerationLimit = maxRegenerationCount ?? int.MaxValue;
            RegenerationCounter = 0;
            ConvergenceRate = convergenceRate ?? 60;
            Population = Enumerable.Range(0, PopulationLenght).Select(r => new Chromosome(ChromosomeLenght).Randomize()).ToArray();
        }

        public Chromosome Start()
        {
            Debug.WriteLine("Başlıyor DA ...");

            while (Evaluation())
            {
                Selection(SelectionPercent);
            }

            return Population.First();  
        }
        
        public bool Evaluation()
        {
            Population = Population.OrderBy(ch => ch.Fitness).ToArray(); // sort 
            var elit = Population.First();
  
            if (Math.Abs(elit.Fitness) < 2)
            {
                Debug.WriteLine("da ended due to the best chromosome found :)");
                return false; // stop da
            }
            if (RegenerationCounter >= RegenerationLimit)
            {
                Debug.WriteLine("da ended due to the limitation of regeneration!!!");
                return false; // stop da
            }
            if (Population.Count(c => Math.Abs(c.Fitness - elit.Fitness) < 1) >= Math.Min((double)ConvergenceRate / 100, 0.9) * PopulationLenght)
            {
                      
                Debug.WriteLine("da ended due to the convergence of chromosomes :(");
                return false;
            }

            return true;  
        }

        public void Regeneration()
        {
            RegenerationCounter++;
            if (RegenerationCounter % 100 == 0)
                Debug.WriteLine("generation {0}, elite fitness is: {1}", RegenerationCounter, Population[0].Fitness);

            var newPopulation = new List<Chromosome>();

            // create new chromosomes 
            for (var index = Population.Length; index < PopulationLenght; index++)
            {
                var parent = GetRandomParent();
                var child = Crossover(parent.mom, parent.dad);
                Mutation(child, MutationProbability);
                child.Evaluate();
                newPopulation.Add(child);
            }

            Population = Population.Concat(newPopulation).ToArray(); 
        }

        public void Selection(int percent)
        {
            var keepCount = percent * PopulationLenght / 100;
            Population = Population.Take(keepCount).ToArray();   
            Regeneration(); // start new generation   
        }

        public void Mutation(Chromosome chromosome, int rate)
        {
           
            if (new Random(0, 100).Next() <= rate)
            { 
                var rand = new Random(0, chromosome.Length - 1);
                var gen1 = rand.Next();
                var gen2 = rand.Next();
                if (gen1 == gen2)
                    throw new Exception("Mutation gens are duplicate!");
                 
                var genBuffer = chromosome.Genome[gen1];
                chromosome.Genome[gen1] = chromosome.Genome[gen2];
                chromosome.Genome[gen2] = genBuffer;
            }
        }

        public Chromosome Crossover(Chromosome mom, Chromosome dad)
        {
            if (mom == dad)
                Debug.WriteLine("Oh shet! are the mom and dad same!?");

            var child = new Chromosome(ChromosomeLenght)
            {
                Genome = Pmx(mom.Genome, dad.Genome)
            };

            return child;
        }

        protected T[] Pmx<T>(T[] mom, T[] dad, int? cut1 = null, int? cut2 = null) where T : struct
        {
            cut1 = cut1 ?? new Random(1, mom.Length / 2).Next();  
            cut2 = cut2 ?? new Random(cut1.Value + 1, mom.Length - 2).Next();   
            var child = new T[mom.Length];
            var usedGenes = new HashSet<T>();
            var childEmptyIndexes = new Stack<int>();

      
            for (var i = cut1.Value; i <= cut2.Value; i++)
            {
                child[i] = mom[i];
                usedGenes.Add(mom[i]); 
            }


            for (var i = 0; i < cut1.Value; i++)
            {
                if (usedGenes.Contains(dad[i]))
                {
                    childEmptyIndexes.Push(i);
                }
                else
                {
                    child[i] = dad[i];
                    usedGenes.Add(dad[i]); 
                }
            }

            for (var i = cut2.Value + 1; i < dad.Length; i++)
            {
                if (usedGenes.Contains(dad[i]))
                {
                    childEmptyIndexes.Push(i);
                }
                else
                {
                    child[i] = dad[i];
                    usedGenes.Add(dad[i]); 
                }
            }
            foreach (var gene in dad.Concat(mom))
            {
                if (childEmptyIndexes.Count == 0)
                    break;

                if (!usedGenes.Contains(gene))
                {
                    child[childEmptyIndexes.Pop()] = gene;
                }
            }

            return child;
        }

        protected (Chromosome mom, Chromosome dad) GetRandomParent()
        {
            var rand = new Random(0, Population.Length - 1);
            return (Population[rand.Next()], Population[rand.Next()]);
        }

    }
}