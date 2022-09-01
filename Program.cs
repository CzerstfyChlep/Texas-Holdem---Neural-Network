using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace TexasHoldemGameEngine
{
    class Program
    {


        public static int NofGamesDone = 0;
        public static int TNumber = 0;

        public static void DisplayNOGames()
        {
            do
            {
                Thread.Sleep(500);
                Console.Clear();
                Console.WriteLine(NofGamesDone);
            } while (true);
        }
        public static async void Train(bool create)
        {
            Console.WriteLine("Creating neural networks");

            int tocreate = 0;
            if (!create)
            {
                if (Directory.Exists("best"))
                {
                    List<string> files = Directory.EnumerateFiles("best").ToList();
                    tocreate = files.Count;
                    NeuralNetworks.AddRange(NeuralNetwork.ReadFromFile("best"));
                }
            }


            for (int a = 0; a < 1000 - tocreate; a++)
            {
                NeuralNetwork n = new NeuralNetwork(rGen);
                n.Create();
                n.SaveToFile("best");
                NeuralNetworks.Add(n);
            }

            for (int c = 0; c < 10; c++)
            {
                NofGamesDone = 0;
                for (int e = 0; e < 100; e++)
                {
                    for (int a = 0; a < 50; a++)
                    {
                        List<NeuralNetwork> NeuralNetworksLeft = new List<NeuralNetwork>();
                        NeuralNetworks.ForEach(x => NeuralNetworksLeft.Add(x));
                        for (int t = 0; t < 10; t++)
                        {

                            for (int g = 0; g < 25; g++)
                            {
                                TexasHoldem.Game game = new TexasHoldem.Game();
                                game.InitialiseGame(4, new int[] { }, 100);
                                int nn = rGen.Next(0, NeuralNetworksLeft.Count);
                                game.Players[0].AssignedNeuralNetwork = NeuralNetworksLeft[nn];
                                NeuralNetworksLeft.Remove(NeuralNetworksLeft[nn]);

                                nn = rGen.Next(0, NeuralNetworksLeft.Count);
                                game.Players[1].AssignedNeuralNetwork = NeuralNetworksLeft[nn];
                                NeuralNetworksLeft.Remove(NeuralNetworksLeft[nn]);

                                nn = rGen.Next(0, NeuralNetworksLeft.Count);
                                game.Players[2].AssignedNeuralNetwork = NeuralNetworksLeft[nn];
                                NeuralNetworksLeft.Remove(NeuralNetworksLeft[nn]);

                                nn = rGen.Next(0, NeuralNetworksLeft.Count);
                                game.Players[3].AssignedNeuralNetwork = NeuralNetworksLeft[nn];
                                NeuralNetworksLeft.Remove(NeuralNetworksLeft[nn]);
                                TNumber++;
                                if (TNumber == 20)
                                    TNumber = 0;
                                Threads[TNumber].GamesToPlay.Add(game);
                                NofGamesDone++;
                            }
                        }            
                    }
                check:
                    if (Threads.Any(x => x.GamesToPlay.Count > 0))
                    {
                        Thread.Sleep(25);
                        goto check;
                    }
                    if (c != 9)
                    {
                        NeuralNetworks = NeuralNetworks.OrderByDescending(x => x.AverageScore).ToList();

                        NeuralNetworks.RemoveRange(NeuralNetworks.Count / 2, NeuralNetworks.Count / 2);
                        List<NeuralNetwork> newList = new List<NeuralNetwork>();
                        foreach (NeuralNetwork n in NeuralNetworks)
                        {
                            NeuralNetwork NewNN = new NeuralNetwork(rGen);
                            NewNN.CopyFromAndAdjust(n);
                            newList.Add(NewNN);
                            newList.Add(n);
                            n.LocalScore = 0;
                        }

                        NeuralNetworks = new List<NeuralNetwork>();
                        NeuralNetworks.AddRange(newList);
                    }
                }
                Directory.EnumerateFiles("best").ToList().ForEach(x => File.Delete(x));
                foreach (NeuralNetwork n in NeuralNetworks)
                {
                    n.SaveToFile("best");
                }
                NeuralNetworks = NeuralNetworks.OrderByDescending(x => x.AverageScore).ToList();
                File.AppendAllText("history.txt", $"\n{NeuralNetworks[0].ID} | {NeuralNetworks[0].AverageScore} | {NeuralNetworks[0].GamesPlayed}");
                Console.WriteLine("iteration " + c + " complete");
            }
        }


        public static List<ApplicationThread> Threads = new List<ApplicationThread>();
        static void Main(string[] args)
        {
            bool keepgoing = true;
            while (keepgoing)
            {
                Console.Clear();
                Console.WriteLine("Type the action you want to execute:\n    -train (trains the networks)\n    -remove [n] (removes n number of worst networks)\n    -play (plays with the best network)\n    -rank (ranks all of the networks)\n    -exit");
                string mode = Console.ReadLine();
                switch (mode)
                {
                    case "train":
                        NeuralNetworks = new List<NeuralNetwork>();
                        for (int a = 0; a < 20; a++)
                        {
                            ApplicationThread at = new ApplicationThread();
                            at.free = true;
                            Threads.Add(at);
                            at.Start();
                        }
                        Thread t = new Thread(DisplayNOGames);
                        t.Start();

                        Train(false);

                        foreach (ApplicationThread at in Threads)
                        {
                            at.terminate = true;
                        }
                        break;

                    default:
                        NeuralNetworks = new List<NeuralNetwork>();
                        if (mode.StartsWith("remove"))
                        {
                            List<string> files = new List<string>();
                            if (Directory.Exists("best"))
                            {
                                files = Directory.EnumerateFiles("best").ToList();
                                NeuralNetworks.AddRange(NeuralNetwork.ReadFromFile("best"));
                            }
                            NeuralNetworks = NeuralNetworks.OrderByDescending(x => x.AverageScore).ToList();
                            int toremove = int.Parse(mode.Split(' ')[1]);
                            NeuralNetworks.RemoveRange(1000 - toremove, toremove);

                            foreach (string f in files)
                            {
                                string newf = f.Replace("best\\", "").Replace(".txt", "");
                                if (!NeuralNetworks.Any(x => x.ID == newf))
                                {
                                    File.Delete(f);
                                }
                            }
                        }
                        break;
                    case "exit":
                        keepgoing = false;
                        break;
                    case "play":
                        NeuralNetworks = new List<NeuralNetwork>();
                        if (Directory.Exists("best"))
                        {
                            List<string> files = Directory.EnumerateFiles("best").ToList();
                            NeuralNetworks.AddRange(NeuralNetwork.ReadFromFile("best"));
                        }
                        NeuralNetworks = NeuralNetworks.OrderByDescending(x => x.AverageScore).ToList();

                        NeuralNetwork opponent = NeuralNetworks.First(x => x.GamesPlayed > 50);
                    gamestart:
                        Console.Clear();

                        Console.WriteLine(opponent.ID);

                        TexasHoldem.Game game = new TexasHoldem.Game();
                        game.InitialiseGame(opponent, 100);
                        game.DoGame();
                        Console.Clear();
                        Console.WriteLine("Game ended!");
                        Console.WriteLine("Community cards:\n");
                        game.CommunityCards.ForEach(x => Console.WriteLine(x.CardToName()));
                        Console.WriteLine("\n---------");
                        Console.WriteLine($"Your cards:\n{game.Players[0].Cards[0].CardToName()}\n{game.Players[0].Cards[1].CardToName()}");
                        Console.WriteLine("\n---------");
                        Console.WriteLine($"Opponents cards:\n{game.Players[1].Cards[0].CardToName()}\n{game.Players[1].Cards[1].CardToName()}");
                        if (Console.ReadLine() == "rematch")
                        {
                            goto gamestart;
                        }
                        break;
                    case "rank":
                        NeuralNetworks = new List<NeuralNetwork>();
                        int n = 0;
                        if (Directory.Exists("best"))
                        {
                            List<string> files = Directory.EnumerateFiles("best").ToList();
                            NeuralNetworks.AddRange(NeuralNetwork.ReadFromFile("best"));
                        }
                        NeuralNetworks = NeuralNetworks.OrderByDescending(x => x.AverageScore).ToList();
                        Console.Clear();
                        string id = "                  ID                  ";
                        string avg = "           Average Score           ";
                        string gm = "       Games Played       ";
                        Console.WriteLine($"{id}|{avg}|{gm}");
                        foreach (NeuralNetwork network in NeuralNetworks)
                        {
                            n++;
                            string numberplusid = $"{n}. {network.ID}";
                            numberplusid = numberplusid.PadRight(id.Length);
                            string avgscore = $"{network.AverageScore}";
                            avgscore = avgscore.PadRight(avg.Length - 1);
                            string gmspla = $"{network.GamesPlayed}";
                            gmspla = gmspla.PadRight(gm.Length);

                            Console.WriteLine($"{numberplusid}| {avgscore}| {gmspla}");

                        }
                        Console.CursorTop = 0;
                        Console.ReadLine();

                        break;
                }


                //best play with a human
                /*
                if (Directory.Exists("best"))
                {
                    List<string> files = Directory.EnumerateFiles("best").ToList();
                    NeuralNetworks.AddRange(NeuralNetwork.ReadFromFile("best"));
                }
                NeuralNetworks = NeuralNetworks.OrderByDescending(x => x.AverageScore).ToList();

                NeuralNetwork opponent = NeuralNetworks[0];

                Console.WriteLine(opponent.ID);
                Console.ReadLine();
                Console.WriteLine(opponent.ID);
                TexasHoldem.Game game = new TexasHoldem.Game();
                game.InitialiseGame(opponent, 100);

                game.DoGame();

                Console.Clear();
                Console.WriteLine("Game ended!");
                Console.WriteLine("Community cards:\n");
                game.CommunityCards.ForEach(x => Console.WriteLine(x.CardToName()));
                Console.WriteLine("\n---------");
                Console.WriteLine($"Your cards:\n{game.Players[0].Cards[0].CardToName()}\n{game.Players[0].Cards[1].CardToName()}");
                Console.WriteLine("\n---------");
                Console.WriteLine($"Opponents cards:\n{game.Players[1].Cards[0].CardToName()}\n{game.Players[1].Cards[1].CardToName()}");

                Console.ReadLine();
                */
            }

        }

        public static List<NeuralNetwork> NeuralNetworks = new List<NeuralNetwork>();
        public static Random rGen = new Random();
        
    }

    public class ApplicationThread
    {
        public bool free = false;
        public Thread t = null;
        public List<TexasHoldem.Game> GamesToPlay = new List<TexasHoldem.Game>();
        public bool terminate = false;
        public void Start()
        {
            t = new Thread(PlayGames);
            t.Start();
        }
        public void PlayGames()
        {
            do
            {
                if (GamesToPlay.Count > 0)
                {
                    TexasHoldem.Game g = GamesToPlay.First();
                    if (g != null)
                    {
                        g.DoGame();
                        GamesToPlay.Remove(g);
                    }
                    else
                        GamesToPlay = new List<TexasHoldem.Game>();
                }
                else
                {
                    Thread.Sleep(25);
                }
            }
            while (!terminate);
        }
    }

    public class TexasHoldem
    {
        public static string[] suits = new string[4] { "D", "C", "H", "S" };
        public static Random RandomGenerator = new Random();
        public class Game
        {
            public List<Player> Players = new List<Player>();
            public List<Player> ParticipatingPlayers = new List<Player>();
            public Deck Deck = new Deck();
            public Player PrevWinner = null;
            public int GameStage = 0;
            public int CommunityBet = 0;
            public int Pot = 0;
            public int WaitEvenPlayers = 0;
            public int BaseMoney = 0;
            public bool WithHuman = false;
            // 0.Entering
            // 1.First betting
            // 2.Second betting
            // 3.Third betting
            // 4.Showdown

            public int PrevStartingPlayer = 0;

            public List<Card> CommunityCards = new List<Card>();

            public Game()
            {
                RandomGenerator = new Random();
            }

            public void InitialiseGame(int PlayersN, int[] Money, int BaseMoney = 100)
            {
                Players = new List<Player>();
                Deck = new Deck();
                this.BaseMoney = BaseMoney;
                for(int a = 0; a < PlayersN; a++)
                {
                    Player p = new Player();
                    p.GameParticipatingIn = this;
                    p.ID = a;
                    p.Money = BaseMoney;
                    if (Money.Any())
                    {
                        if (Money.Count() >= a + 1)
                            p.Money = Money[a];
                        else
                            p.Money = BaseMoney;
                    }
                    else
                        p.Money = BaseMoney;
                    Players.Add(p);
                }
            }

            public void InitialiseGame(NeuralNetwork n, int BaseMoney = 100)
            {
                Players = new List<Player>();
                Deck = new Deck();
                this.BaseMoney = BaseMoney;

                Player human = new Player();
                human.GameParticipatingIn = this;
                human.ID = 0;
                human.Money = BaseMoney;
                human.Human = true;
                Players.Add(human);

                Player network = new Player();
                network.GameParticipatingIn = this;
                network.ID = 1;
                network.Money = BaseMoney;
                network.Human = false;
                network.AssignedNeuralNetwork = n;
                Players.Add(network);

            }

            public void DoGame(int EntryFee = 2)
            {
                Deck = new Deck();
                CommunityCards = new List<Card>();
                ParticipatingPlayers = new List<Player>();
                List<Player> WinningP = new List<Player>();
                CommunityBet = EntryFee;
                foreach (Player p in Players)
                {
                    if (p.Money >= EntryFee)
                    {
                        ParticipatingPlayers.Add(p);
                        p.CurrentBet = 0;
                        Pot += EntryFee;
                        p.Money -= EntryFee;
                        p.Cards = new List<Card>
                        {
                            Deck.GetRandomCard(),
                            Deck.GetRandomCard()
                        };
                    }
                    else
                        p.Participating = false;
                }
                //Console.WriteLine("Players added");
                //Console.WriteLine("Participating players: " + ParticipatingPlayers.Count);
                //Console.WriteLine("Continue...");
                //Console.ReadLine();
                //Console.Clear();


                CommunityCards.Add(Deck.GetRandomCard());
                CommunityCards.Add(Deck.GetRandomCard());
                CommunityCards.Add(Deck.GetRandomCard());


                


                Pot = 0;
                CommunityBet = 0;
                WaitEvenPlayers = 0;

                GameStage = 1;
                AdvanceGame();
                if (ParticipatingPlayers.Count < 2)
                {
                    if (WinningP.Any())
                    {
                        WinningP.Add(ParticipatingPlayers[0]);
                        goto fastend;
                    }
                    else
                    {
                        goto totalend;
                    }
                }


                CommunityCards.Add(Deck.GetRandomCard());
                CommunityBet = 0;
                WaitEvenPlayers = 0;

                GameStage = 2;
                AdvanceGame();
                if (ParticipatingPlayers.Count < 2)
                {
                    if (WinningP.Any())
                    {
                        WinningP.Add(ParticipatingPlayers[0]);
                        goto fastend;
                    }
                    else
                    {
                        goto totalend;
                    }
                }

                CommunityCards.Add(Deck.GetRandomCard());
                CommunityBet = 0;
                WaitEvenPlayers = 0;

                GameStage = 3;
                AdvanceGame();
                if (ParticipatingPlayers.Count < 2)
                {
                    if (WinningP.Any())
                    {
                        WinningP.Add(ParticipatingPlayers[0]);
                        goto fastend;
                    }
                    else
                    {
                        goto totalend;
                    }
                }

                GameStage = 4;

                WinningP = ConcludeGame();

                fastend:
                if(WinningP.Count == 1)
                {
                    WinningP[0].Money += Pot;
                }
                else
                {
                    foreach(Player c in WinningP)
                    {
                        c.Money += (int)Math.Ceiling(Pot / (WinningP.Count * 1.0));
                    }
                }


                PrevWinner = WinningP[0];

                foreach(Player p in Players)
                {
                    if (!p.Human)
                    {
                        p.AssignedNeuralNetwork.LocalScore += p.Money - BaseMoney;
                        p.AssignedNeuralNetwork.OverallScore += p.Money - BaseMoney;
                        p.AssignedNeuralNetwork.GamesPlayed++;
                    }
                }
            //Console.WriteLine("Game ended");
            //Console.WriteLine($"Cards: {CommunityCards[0].CardToName()}, {CommunityCards[1].CardToName()}, {CommunityCards[2].CardToName()}, {CommunityCards[3].CardToName()}, {CommunityCards[4].CardToName()}");
            //Console.WriteLine("Community cards: " + CommunityCards.Count());
            //Players.ForEach(x => Console.WriteLine($"Player #{x.ID}: {x.Cards[0].CardToName()}, {x.Cards[1].CardToName()} - {x.BestHand.ScoreToHandName()}"));
            //WinningP.ForEach(x=>Console.WriteLine($"Player #{x.ID} won with {x.BestHand.ScoreToHandName()}"));

            // Console.WriteLine("\n\n\n\n");
            // WinningP[0].Hands.ForEach(x => Console.WriteLine($"\n{x.ScoreToHandName()} ({x.Cards[0].CardToName()})"));
            // Console.WriteLine("Continue...");
            // Console.ReadLine();
            // Console.Clear();
            totalend:
                ;
            }

            public void AdvanceGame(int startingPlayer = 0)
            {
                int currentplayer = startingPlayer;
                //Betting
                do
                {
                    Player p = ParticipatingPlayers[currentplayer];
                    string action = p.AskForAction();
                    int addtobet = 0;
                    switch (action)
                    {
                        case "even":                                                          
                            if ((p.CurrentBet >= CommunityBet || (p.CurrentBet < CommunityBet && p.Money == 0)) && addtobet == 0)
                                    WaitEvenPlayers++;
                            else
                            {
                                int difference = CommunityBet - p.CurrentBet;
                                if (difference > p.Money)
                                {
                                    p.CurrentBet += p.Money;
                                    Pot += p.Money;
                                    p.Money = 0;
                                   
                                }
                                else
                                {
                                    p.CurrentBet += difference;
                                    Pot += difference;
                                    p.Money -= difference;
                                }
                                
                                if (addtobet != 0)
                                {
                                    int toadd = 0;
                                    if (addtobet == 1)
                                    {
                                        toadd = (int)Math.Ceiling(p.Money * 0.05);
                                    }
                                    else if (addtobet == 2)
                                    {
                                        toadd = (int)Math.Ceiling(p.Money * 0.1);
                                    }
                                    else if (addtobet == 3)
                                    {
                                        toadd = (int)Math.Ceiling(p.Money * 0.2);
                                    }
                                    else if (addtobet == 4)
                                    {
                                        toadd = (int)Math.Ceiling(p.Money * 0.5);
                                    }
                                    else if (addtobet == 5)
                                    {
                                        toadd = p.Money;
                                    }

                                    if (p.Money == 0 || toadd == 0)
                                    {
                                        WaitEvenPlayers++;
                                        break;
                                    }

                                    if (toadd > p.Money)
                                    {
                                        CommunityBet += p.Money;
                                        p.CurrentBet += p.Money;
                                        Pot += p.Money;
                                        p.Money = 0;
                                    }
                                    else
                                    {
                                        CommunityBet += toadd;
                                        p.CurrentBet += toadd;
                                        Pot += toadd;
                                        p.Money -= toadd;
                                    }
                                    WaitEvenPlayers = 1;
                                }
                                else
                                    WaitEvenPlayers++;
                            }
                            break;
                        case "fold":
                            ParticipatingPlayers.Remove(p);
                            p.Participating = false;                               
                            break;
                        case "raise5":
                            addtobet = 1;
                            goto case "even";
                        case "raise10":
                            addtobet = 2;
                            goto case "even";
                        case "raise20":
                            addtobet = 3;
                            goto case "even";
                        case "raise50":
                            addtobet = 4;
                            goto case "even";
                        case "raise100":
                            addtobet = 5;
                            goto case "even";
                        default:
                            goto case "even";
                    }
                    if (ParticipatingPlayers.Count() <= currentplayer + 1)
                        currentplayer = 0;
                    else
                        currentplayer++;

                }
                while (WaitEvenPlayers < ParticipatingPlayers.Count && ParticipatingPlayers.Count > 1);
                foreach(Player p in Players)
                {
                    p.CurrentBet = 0;
                }
                
            }

            public List<Player> ConcludeGame()
            {
                List<Hand> Hands = new List<Hand>();
                foreach(Player p in ParticipatingPlayers)
                {
                    List<Hand> PlayerHands = new List<Hand>();
                    List<Card> CardsInPlay = new List<Card>();
                    CardsInPlay.AddRange(CommunityCards);
                    CardsInPlay.AddRange(p.Cards);
                    CardsInPlay = CardsInPlay.OrderByDescending(x => x.N).ToList();
                    foreach(Card c in CardsInPlay)
                    {
                        //straight and straight flush
                        Card[] onebelow = CardsInPlay.FindAll(x => x.N == c.N - 1).ToArray();
                        if (!onebelow.Any())
                        {
                            goto skipstraightcounting;
                        }
                        Card[] twobelow = CardsInPlay.FindAll(x => x.N == c.N - 2).ToArray();
                        if (!twobelow.Any())
                        {
                            goto skipstraightcounting;
                        }
                        Card[] threebelow = CardsInPlay.FindAll(x => x.N == c.N - 3).ToArray();
                        if (!threebelow.Any())
                        {
                            goto skipstraightcounting;
                        }
                        Card[] fourbelow = CardsInPlay.FindAll(x => x.N == c.N - 4).ToArray();
                        if(!fourbelow.Any())
                        {
                            goto skipstraightcounting;
                        }

                        if(onebelow.Any(x=>x.S == c.S) && twobelow.Any(x => x.S == c.S) && threebelow.Any(x => x.S == c.S) && fourbelow.Any(x => x.S == c.S))
                        {
                            Hand h = new Hand();
                            h.Owner = p;
                            h.Score = 8;
                            h.Cards.AddRange(new Card[] { c, onebelow.ToList().Find(x=>x.S == c.S), twobelow.ToList().Find(x => x.S == c.S), threebelow.ToList().Find(x => x.S == c.S), fourbelow.ToList().Find(x => x.S == c.S) });
                            PlayerHands.Add(h);
                        }
                        else
                        {
                            Hand h = new Hand();
                            h.Owner = p;
                            h.Score = 4;
                            h.Cards.AddRange(new Card[] { c, onebelow[0], twobelow[0], threebelow[0], fourbelow[0] });
                            PlayerHands.Add(h);
                        }
                    skipstraightcounting:
                        //flush
                        List<Card> CardsWithSameSuit = CardsInPlay.FindAll(x => x.S == c.S);
                        if (CardsWithSameSuit.Count >= 5)
                        {
                            Hand h = new Hand();
                            h.Owner = p;
                            h.Score = 5;
                            h.Cards.AddRange(CardsWithSameSuit);
                            PlayerHands.Add(h);
                        }
                        //high card
                        

                        //four/three of a kind and pair
                        List<Card> SameNCards = CardsInPlay.FindAll(x => x.N == c.N);
                        if(SameNCards.Count == 2)
                        {
                            if(!PlayerHands.Any(x=>x.Score == 1 && x.Cards[0].N == c.N))
                            {
                                Hand h = new Hand();
                                h.Owner = p;
                                h.Score = 1;
                                h.Cards.AddRange(SameNCards);
                                PlayerHands.Add(h);
                            }
                        }
                        else if(SameNCards.Count == 3)
                        {
                            if (!PlayerHands.Any(x => x.Score == 3 && x.Cards[0].N == c.N))
                            {
                                Hand h = new Hand();
                                h.Owner = p;
                                h.Score = 3;
                                h.Cards.AddRange(SameNCards);
                                PlayerHands.Add(h);
                            }
                        }
                        else if (SameNCards.Count == 4)
                        {
                            if (!PlayerHands.Any(x => x.Score == 4 && x.Cards[0].N == c.N))
                            {
                                Hand h = new Hand();
                                h.Owner = p;
                                h.Score = 7;
                                h.Cards.AddRange(SameNCards);
                                PlayerHands.Add(h);
                            }
                        }
                    }
                    //high card
                    if (p.Cards[0].N > p.Cards[1].N)
                    {
                        Hand h = new Hand();
                        h.Owner = p;
                        h.Score = 0;
                        h.Cards.Add(p.Cards[0]);
                        PlayerHands.Add(h);
                    }
                    else
                    {
                        Hand h = new Hand();
                        h.Owner = p;
                        h.Score = 0;
                        h.Cards.Add(p.Cards[1]);
                        PlayerHands.Add(h);
                    }

                    List<Hand> PairHands = PlayerHands.FindAll(x => x.Score == 1);
                    if (PairHands.Count >= 2)
                    {                 
                        Hand h = new Hand();
                        h.Owner = p;
                        h.Score = 2;
                        PairHands = PairHands.OrderByDescending(x => x.Cards[0].N).ToList();
                        h.Cards.AddRange(PairHands[0].Cards);
                        h.Cards.AddRange(PairHands[1].Cards);
                        PlayerHands.Add(h);
                    }
                    Hand ThreeOfAKind = PlayerHands.Find(x => x.Score == 3);
                    if(PairHands.Count >= 2 && ThreeOfAKind != null)
                    {
                        Hand h = new Hand();
                        h.Owner = p;
                        h.Score = 6;
                        h.Cards.AddRange(ThreeOfAKind.Cards);
                        PairHands = PairHands.OrderByDescending(x => x.Cards[0].N).ToList();
                        h.Cards.AddRange(PairHands[0].Cards);
                        PlayerHands.Add(h);
                    }
                    PlayerHands = PlayerHands.OrderByDescending(x => x.Score).ThenByDescending(y=>y.Cards[0].N).ToList();
                    p.BestHand = PlayerHands[0];
                    p.Hands.AddRange(PlayerHands);
                    Hands.Add(PlayerHands[0]);
                }
                Hands = Hands.OrderByDescending(x => x.Score).ToList();
                //Hands.ForEach(x => Console.WriteLine(x.ScoreToHandName() + " - " + x.Owner.ID));
                //Console.WriteLine("----");
                Hands.RemoveAll(x => x.Score < Hands[0].Score);
                //Hands.ForEach(x => Console.WriteLine(x.ScoreToHandName() + " - " + x.Owner.ID));
                //Console.ReadLine();
                List<Player> WinningPlayers = new List<Player>();
                if(Hands.Any(x=>x.Score == Hands[0].Score && x != Hands[0]))
                {
                    switch (Hands[0].Score)
                    {
                        case 2:
                            Hands.ForEach(x => x.Cards = x.Cards.OrderByDescending(y => y.N).ToList());
                            Hands = Hands.OrderByDescending(x => x.Cards[0].N).ToList();                            
                            if (Hands.All(x => x.Cards[0].N < Hands[0].Cards[0].N || Hands[0] == x))
                            {
                                WinningPlayers.Add(Hands[0].Owner);
                            }
                            else
                            {
                                List<Hand> WinningHands = (Hands.FindAll(x => x.Cards[0].N == Hands[0].Cards[0].N));
                                WinningHands = WinningHands.OrderByDescending(x => x.Cards[0].N).ThenByDescending(x => x.Cards[2].N).ToList();
                                if(!WinningHands.Any(x=> x != WinningHands[0] && x.Cards[2] == WinningHands[0].Cards[2]))
                                {
                                    WinningPlayers.Add(WinningHands[0].Owner);
                                }
                                else
                                {
                                    List<Hand> FinalWinningHands = (Hands.FindAll(x => x.Cards[0].N == Hands[0].Cards[0].N && x.Cards[2].N == Hands[0].Cards[2].N));
                                    FinalWinningHands.ForEach(x => WinningPlayers.Add(x.Owner));
                                }
                            }
                            break;
                        case 5:
                            Hands.ForEach(x => WinningPlayers.Add(x.Owner));
                            break;
                        case 6:
                            Hands.ForEach(x => x.Cards = x.Cards.OrderByDescending(y => y.N).ToList());
                            Hands = Hands.OrderByDescending(x => x.Cards[0].N + x.Cards[4].N).ToList();
                            List<Hand> WinningHands6 = Hands.FindAll(x => x == Hands[0] || Hands[0].Cards[0].N + Hands[0].Cards[4].N == x.Cards[0].N + x.Cards[4].N);
                            WinningHands6.ForEach(x => WinningPlayers.Add(x.Owner));
                            break;
                        default:
                            Hands = Hands.OrderByDescending(x => x.Cards[0].N).ToList();
                            if (Hands.All(x => x.Cards[0].N < Hands[0].Cards[0].N || x == Hands[0]))
                            {
                                WinningPlayers.Add(Hands[0].Owner);
                            }
                            else
                            {
                                List<Hand> WinningHands = (Hands.FindAll(x => x.Cards[0].N == Hands[0].Cards[0].N));
                                WinningHands.ForEach(x => WinningPlayers.Add(x.Owner));
                            }
                            break;

                    }
                }
                else
                {
                    WinningPlayers.Add(Hands[0].Owner);
                }

                if (WithHuman)
                {
                    Console.Clear();
                    Console.WriteLine("Game ended!");
                    Console.WriteLine("Community cards:\n");
                    CommunityCards.ForEach(x => Console.WriteLine(x.CardToName()));
                    Console.WriteLine("\n---------");
                    Console.WriteLine($"Your cards:\n{Players[0].Cards[0].CardToName()}\n{Players[0].Cards[1].CardToName()}");
                    Console.WriteLine("\n---------");
                    Console.WriteLine($"Opponents cards:\n{Players[1].Cards[0].CardToName()}\n{Players[1].Cards[1].CardToName()}");
                }

                return WinningPlayers;
            }
        }
        public class Player
        {
            public List<Card> Cards = new List<Card>();
            public Game GameParticipatingIn = null;
            public Hand BestHand = new Hand();
            public List<Hand> Hands = new List<Hand>();
            public NeuralNetwork AssignedNeuralNetwork = null;
            public bool Human = false;
            public int ID = 0;
            public int Money = 0;
            public bool Participating = true;
            public int CurrentBet = 0;

            public string AskForAction()
            {
                if (!Human)
                    return AssignedNeuralNetwork.AskForAction(this);
                else
                {
                    Console.Clear();
                    Console.WriteLine("Community cards:\n");
                    GameParticipatingIn.CommunityCards.ForEach(x => Console.WriteLine(x.CardToName()));
                    Console.WriteLine("\n---------");
                    Console.WriteLine($"Your cards:\n\n{Cards[0].CardToName()}\n{Cards[1].CardToName()}");
                    Console.WriteLine("\n---------");
                    Console.WriteLine($"Your money: {Money}\nCommunity bet: {GameParticipatingIn.CommunityBet}      |   Your bet: {CurrentBet}");
                    Console.WriteLine("\n---------\nNeural Network outputs (from previous choice):");
                    Console.WriteLine($"Wait/Even: {GameParticipatingIn.Players[1].AssignedNeuralNetwork.OutputNeurons[0]}");
                    Console.WriteLine($"Fold: {GameParticipatingIn.Players[1].AssignedNeuralNetwork.OutputNeurons[1]}");
                    Console.WriteLine($"Raise 5%: {GameParticipatingIn.Players[1].AssignedNeuralNetwork.OutputNeurons[2]}");
                    Console.WriteLine($"Raise 10%: {GameParticipatingIn.Players[1].AssignedNeuralNetwork.OutputNeurons[3]}");
                    Console.WriteLine($"Raise 20%: {GameParticipatingIn.Players[1].AssignedNeuralNetwork.OutputNeurons[4]}");
                    Console.WriteLine($"Raise 50%: {GameParticipatingIn.Players[1].AssignedNeuralNetwork.OutputNeurons[5]}");
                    Console.WriteLine($"All-In: {GameParticipatingIn.Players[1].AssignedNeuralNetwork.OutputNeurons[6]}");
                    //GameParticipatingIn.Players[1].AssignedNeuralNetwork.OutputNeurons.ToList().ForEach(x => Console.WriteLine(x));
                    Console.WriteLine("\n---------\nYour choice (commands: even, fold, raise5, raise10, raise20, raise50, raise100)");
                    return Console.ReadLine();
                }
            }
            // 0. Wait / Even up
            // 1. Fold
            // 2. Raise 5%
            // 3. Raise 10%
            // 4. Raise 20%
            // 5. Raise 50%
            // 6. Raise 100%
        }
        public class Card
        {
            public int N;
            public string S;
            public Card(int N, string S)
            {
                this.N = N;
                this.S = S;
            }
            public string CardToName()
            {
                string fullname = "";
                if (this.N < 11)
                    fullname += this.N;
                else if (this.N == 11)
                    fullname += "Jack";
                else if (this.N == 12)
                    fullname += "Queen";
                else if (this.N == 13)
                    fullname += "King";
                else if (this.N == 14)
                    fullname += "Ace";
                fullname += " of";
                if (this.S == "H")
                    fullname += " Hearts";
                else if (this.S == "S")
                    fullname += " Spades";
                else if (this.S == "C")
                    fullname += " Clubs";
                else if (this.S == "D")
                    fullname += " Diamonds";
                return fullname;
            }

            public double CardToAIValue()
            {
                int n = (N - 2) * 4;
                switch (S)
                {
                    case "H":
                        n += 1;
                        break;
                    case "S":
                        n += 2;
                        break;
                    case "C":
                        n += 3;
                        break;
                    case "D":
                        n += 4;
                        break;
                }
                return n;
            }
        }
        public class Hand
        {
            public List<Card> Cards = new List<Card>();
            public int Score = 0;
            public Player Owner = null;          
            public  string ScoreToHandName()
            {
                switch (Score)
                {
                    case 0:
                        return "High Card";
                    case 1:
                        return "Pair";
                    case 2:
                        return "Two Pair";
                    case 3:
                        return "Three of a Kind";
                    case 4:
                        return "Straight";
                    case 5:
                        return "Flush";
                    case 6:
                        return "Full House";
                    case 7:
                        return "Four of a Kind";
                    case 8:
                        return "Straight Flush";
                    case 9:
                        return "Royal Flush";
                    default:
                        return "Error";
                }
            }
        }
        public class Deck
        {
            public List<Card> Cards = new List<Card>();
            public Deck()
            {
                for (int s = 0; s < 4; s++)
                {
                    for (int n = 2; n < 15; n++)
                    {
                        Cards.Add(new Card(n, suits[s]));
                    }
                }
            }

            public void ReplenishDeck()
            {
                Cards = new List<Card>();
                for (int s = 0; s < 4; s++)
                {
                    for (int n = 2; n < 15; n++)
                    {
                        Cards.Add(new Card(n, suits[s]));
                    }
                }
            }

            public Card GetRandomCard()
            {
                if (Cards.Any())
                {
                    Card c = Cards[RandomGenerator.Next(0, Cards.Count())];
                    Cards.Remove(c);
                    return c;
                }
                else
                    return null;
            }
        }
    }

    public class NeuralNetwork
    {
        public int LocalScore = 0;
        public int OverallScore = 0;
        public int GamesPlayed = 0;
        public double AverageScore
        {
            get
            {
                return OverallScore / (double)GamesPlayed;
            }
        }

        public string ID = "";

        public double[] InputNeurons = new double[11];
        public double[,] InputWeights = new double[11,50];

        //   |
        //   |
        //   V

        public double[] MiddleNeurons = new double[50];
        public double[] MiddleNeuronsBiases = new double[50];
        public double[,] MiddleNeuronsWeights = new double[50, 50];

        //   |
        //   |
        //   V

        public double[] SecondMiddleNeurons = new double[50];
        public double[] SecondMiddleNeuronsBiases = new double[50];
        public double[,] SecondMiddleNeuronsWeights = new double[50, 7];

        //   |
        //   |
        //   V

        public double[] OutputNeurons = new double[7];
        Random rGen = new Random();

        public NeuralNetwork(Random randomGenerator)
        {
            rGen = randomGenerator;
        }

        public void CopyFrom(NeuralNetwork n)
        {
            string familiyName = n.ID.Split('.')[0];
            string modelName = rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString();
            ID = familiyName + "." + modelName;
            for (int a = 0; a < 11; a++)
            {
                for (int b = 0; b < 20; b++)
                {
                    InputWeights[a, b] = n.InputWeights[a, b];
                }
            }
            for (int a = 0; a < 20; a++)
            {
                MiddleNeuronsBiases[a] = n.MiddleNeuronsBiases[a];
                for (int b = 0; b < 20; b++)
                {
                    MiddleNeuronsWeights[a, b] = n.MiddleNeuronsWeights[a, b];
                }
            }
            for (int a = 0; a < 20; a++)
            {
                SecondMiddleNeuronsBiases[a] = n.SecondMiddleNeuronsBiases[a];
                for (int b = 0; b < 7; b++)
                {
                    SecondMiddleNeuronsWeights[a, b] = n.SecondMiddleNeuronsWeights[a, b];
                }
            }
        }

        public void CopyFromAndAdjust(NeuralNetwork n)
        {
            string familiyName = n.ID.Split('.')[0];
            string modelName = rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString();
            ID = familiyName + "." + modelName;
            for (int a = 0; a < 11; a++)
            {
                for (int b = 0; b < 50; b++)
                {
                    InputWeights[a, b] = n.InputWeights[a,b];
                }
            }
            for (int a = 0; a < 50; a++)
            {
                MiddleNeuronsBiases[a] = n.MiddleNeuronsBiases[a];
                for (int b = 0; b < 50; b++)
                {
                    MiddleNeuronsWeights[a, b] = n.MiddleNeuronsWeights[a,b];
                }
            }
            for (int a = 0; a < 50; a++)
            {
                SecondMiddleNeuronsBiases[a] = n.SecondMiddleNeuronsBiases[a];
                for (int b = 0; b < 7; b++)
                {
                    SecondMiddleNeuronsWeights[a, b] = n.SecondMiddleNeuronsWeights[a,b];
                }
            }
            Adjust(1);
        }

        public void Create()
        {
            string familiyName = rGen.Next(0,10).ToString() + rGen.Next(0, 10).ToString()+ rGen.Next(0, 10).ToString()+ rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString();
            string modelName = rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString() + rGen.Next(0, 10).ToString();
            ID = familiyName + "." + modelName;

            for (int a = 0; a < 11; a++)
            {
                for (int b = 0; b < 50; b++)
                {                  
                    InputWeights[a, b] = (rGen.NextDouble() - 0.5);                    
                }
            }
            for (int a = 0; a < 50; a++)
            {                
                MiddleNeuronsBiases[a] = (rGen.NextDouble() - 0.5);               
                for (int b = 0; b < 50; b++)
                {                   
                    MiddleNeuronsWeights[a, b] = (rGen.NextDouble() - 0.5) * 0.1;                    
                }
            }
            for (int a = 0; a < 50; a++)
            {                                
                SecondMiddleNeuronsBiases[a] = (rGen.NextDouble() - 0.5);              
                for (int b = 0; b < 7; b++)
                {                    
                    SecondMiddleNeuronsWeights[a, b] = (rGen.NextDouble() - 0.5) * 0.1;                    
                }
            }
        }

        public void Adjust(int TweakLevel = 3)
        {
            int WeightChangeChance = 0;
            switch (TweakLevel)
            {
                case 1:
                    WeightChangeChance = 1;
                    break;
                case 2:
                    WeightChangeChance = 2;
                    break;
                case 3:
                    WeightChangeChance = 4;
                    break;
                case 4:
                    WeightChangeChance = 6;
                    break;
                case 5:
                    WeightChangeChance = 10;
                    break;
            }
            for(int a = 0; a < 11; a++)
            {
                for(int b= 0; b<50; b++)
                {
                    if(rGen.Next(1, 100) <= WeightChangeChance)
                    {
                        InputWeights[a, b] += (rGen.NextDouble() - 0.5) * 0.1;
                    }
                }
            }
            for(int a = 0; a<50; a++)
            {
                if (rGen.Next(1, 100) <= WeightChangeChance)
                {
                    MiddleNeuronsBiases[a] += (rGen.NextDouble() - 0.5) * 0.1;
                }
                for (int b = 0; b < 50; b++)
                {
                    if (rGen.Next(1, 100) <= WeightChangeChance)
                    {
                        MiddleNeuronsWeights[a, b] += (rGen.NextDouble() - 0.5) * 0.1;
                    }
                }
            }
            for (int a = 0; a < 50; a++)
            {
                if (rGen.Next(1, 100) <= WeightChangeChance)
                {
                    SecondMiddleNeuronsBiases[a] += (rGen.NextDouble() - 0.5) * 0.1;
                }
                for (int b = 0; b < 7; b++)
                {
                    if (rGen.Next(1, 100) <= WeightChangeChance)
                    {
                        SecondMiddleNeuronsWeights[a, b] += (rGen.NextDouble() - 0.5) * 0.1;
                    }
                }
            }

        }
        public string AskForAction(TexasHoldem.Player player)
        {

            //setting input neurons
            InputNeurons[0] = player.Cards[0].CardToAIValue();
            InputNeurons[1] = player.Cards[1].CardToAIValue();
            TexasHoldem.Game game = player.GameParticipatingIn;
            if (game.CommunityCards.Count > 0)
                InputNeurons[2] = game.CommunityCards[0].CardToAIValue();
            else
                InputNeurons[2] = 0;
            if (game.CommunityCards.Count > 1)
                InputNeurons[3] = game.CommunityCards[1].CardToAIValue();
            else
                InputNeurons[3] = 0;
            if (game.CommunityCards.Count > 2)            
                InputNeurons[4] = game.CommunityCards[2].CardToAIValue();
            else
                InputNeurons[4] = 0;
            if (game.CommunityCards.Count > 3)            
                InputNeurons[5] = game.CommunityCards[3].CardToAIValue();
            else
                InputNeurons[5] = 0;
            if (game.CommunityCards.Count > 4)            
                InputNeurons[6] = game.CommunityCards[4].CardToAIValue();
            else
                InputNeurons[6] = 0;
            InputNeurons[7] = player.Money / game.BaseMoney;
            if (player.CurrentBet != 0)
                InputNeurons[8] = game.CommunityBet / player.CurrentBet;
            else
                InputNeurons[8] = 0;
            InputNeurons[9] = player.CurrentBet / game.BaseMoney;
            InputNeurons[10] = game.GameStage;
           
            //calculating middle neurons

            for(int aM =0; aM< 50; aM++)
            {
                double sum = -MiddleNeuronsBiases[aM];
                for (int aI = 0; aI < 11; aI++)
                {
                    sum += InputNeurons[aI] * InputWeights[aI, aM];
                }
                MiddleNeurons[aM] = Math.Tanh(sum);
            }

            for (int aM = 0; aM < 50; aM++)
            {
                double sum = -SecondMiddleNeuronsBiases[aM];
                for (int aI = 0; aI < 50; aI++)
                {
                    sum += MiddleNeurons[aI] * MiddleNeuronsWeights[aI, aM];
                }
                SecondMiddleNeurons[aM] = Math.Tanh(sum);
            }

            for (int aO = 0; aO < 7; aO++)
            {
                double sum = 0;
                for (int aM = 0; aM < 50; aM++)
                {
                    sum += SecondMiddleNeurons[aM] * MiddleNeuronsWeights[aM, aO];
                }
                OutputNeurons[aO] = Math.Tanh(sum);
            }
            int result = 0;
            for(int a = 0; a< 7; a++)
            {

                if (OutputNeurons.All(x => x <= OutputNeurons[a]))
                    result = a;
            }
            switch (result)
            {
                case 0:
                    return "even";
                case 1:
                    return "fold";
                case 2:
                    return "raise5";
                case 3:
                    return "raise10";
                case 4:
                    return "raise20";
                case 5:
                    return "raise50";
                case 6:
                    return "raise100";
                default:
                    return "even";
            }
        }
        public void SaveToFile(string folderLocation)
        {
            if (!Directory.Exists(folderLocation))
            {
                Directory.CreateDirectory(folderLocation);
            }            
            string path = folderLocation + "\\" + ID + ".txt";
            string ToWrite = "";
            ToWrite += "id:" + ID; 
            ToWrite += "\nos:" + OverallScore;
            ToWrite += "\ngp:" + GamesPlayed;
            for (int a = 0; a < 11; a++)
            {
                for (int b = 0; b < 50; b++)
                {
                    ToWrite += $"\niw:{a}${b}${InputWeights[a, b]}";
                }
            }
            for (int a = 0; a < 50; a++)
            {
                ToWrite += $"\nmb:{a}${MiddleNeuronsBiases[a]}";
                for (int b = 0; b < 50; b++)
                {
                    ToWrite += $"\nmw:{a}${b}${MiddleNeuronsWeights[a, b]}";
                }
            }
            for (int a = 0; a < 50; a++)
            {
                ToWrite += $"\nsb:{a}${SecondMiddleNeuronsBiases[a]}";
                for (int b = 0; b < 7; b++)
                {
                    ToWrite += $"\nsw:{a}${b}${SecondMiddleNeuronsWeights[a, b]}";
                }
            }
            File.WriteAllText(path, ToWrite);
        }

        public static List<NeuralNetwork> ReadFromFile(string folderLocation)
        {
            List<string> files = Directory.EnumerateFiles(folderLocation).ToList();
            List<NeuralNetwork> networks = new List<NeuralNetwork>();
            foreach(string s in files)
            {
                NeuralNetwork network = new NeuralNetwork(Program.rGen);
                List<string> text = File.ReadAllLines(s).ToList();
                foreach (string line in text)
                {
                    if (line.StartsWith("id:"))
                    {
                        network.ID = line.Split(':')[1];
                    }
                    else if (line.StartsWith("os:"))
                    {
                        network.OverallScore = int.Parse(line.Split(':')[1]);
                    }
                    else if (line.StartsWith("gp:"))
                    {
                        network.GamesPlayed = int.Parse(line.Split(':')[1]);
                    }
                    else if (line.StartsWith("iw:"))
                    {
                        string value = line.Split(':')[1];
                        string[] values = value.Split('$');
                        network.InputWeights[int.Parse(values[0]), int.Parse(values[1])] = double.Parse(values[2]);
                    }
                    else if (line.StartsWith("mw:"))
                    {
                        string value = line.Split(':')[1];
                        string[] values = value.Split('$');
                        network.MiddleNeuronsWeights[int.Parse(values[0]), int.Parse(values[1])] = double.Parse(values[2]);
                    }
                    else if (line.StartsWith("sw:"))
                    {
                        string value = line.Split(':')[1];
                        string[] values = value.Split('$');
                        network.SecondMiddleNeuronsWeights[int.Parse(values[0]), int.Parse(values[1])] = double.Parse(values[2]);
                    }
                    else if (line.StartsWith("mb:"))
                    {
                        string value = line.Split(':')[1];
                        string[] values = value.Split('$');
                        network.MiddleNeuronsBiases[int.Parse(values[0])] = double.Parse(values[1]);
                    }
                    else if (line.StartsWith("sb:"))
                    {
                        string value = line.Split(':')[1];
                        string[] values = value.Split('$');
                        network.SecondMiddleNeuronsBiases[int.Parse(values[0])] = double.Parse(values[1]);
                    }
                }
                networks.Add(network);
            }
            return networks;
        }

    }

    
}
