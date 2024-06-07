namespace HangarbackSim
{
    enum WalkerAStates
    {
        Untapped,
        Tapped,
        Yard,
        Deck,
        Hand,
    }

    enum ElixirStates
    {
        Hand,
        Play,
        Deck
    }

    enum StateEval
    {
        Unknown,
        Draw,
        P1Win,
        P2Win,
    }

    struct ToDo
    {
        public GameState state;
        public bool finishProcessing;

        public void Do(Stack<ToDo> todo)
        {
            StateEval eval = state.TryResolve(todo, finishProcessing);
        }
    }

    class MainC
    {
        public static void Main()
        {
            Console.WriteLine("Initializing...");
            PopulateWinStates();
            Console.WriteLine($"{GameState.NumStatesCompleted} win states");

            // P2 goes first:
            // initial play: City,WalkerB | City,WalkerA | WalkerC | Elixir, pump WalkerA |
            GameState startState = new GameState()
            {
                WhoseTurn = 1,
                WalkerASize = 2,
                WalkerAState = (byte)(WalkerAStates.Tapped),
                WalkerBSize = 1,
                WalkerCSize = 1,
                Elixir = (byte)ElixirStates.Play,
                P1Life = 20,
                P2Life = 20,
            };
            /* //P1 goes first
            // initial play: City,WalkerA | City,WalkerB | Elixir, pump WalkerA | WalkerC, swing for 1 | pass |
            GameState startState = new GameState()
            {
                WhoseTurn = 1,
                WalkerASize = 2,
                WalkerBSize = 1,
                WalkerCSize = 1,
                Elixir = (byte)ElixirStates.Play,
                P1Life = 19,
                P2Life = 20,
            };*/
            Console.WriteLine($"Evaluating {startState.ToString()}...");
            Stack<ToDo> todo = new Stack<ToDo>(capacity: 100000);
            startState.TryResolve(todo);
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                if (todo.TryPop(out ToDo nextAction))
                    nextAction.Do(todo);
                else
                    break;

                if (sw.ElapsedMilliseconds > 3000)
                {
                    Console.WriteLine($"{GameState.NumStatesCompleted} states complete");
                    sw.Restart();
                }
            }
            Console.WriteLine($"Result: {startState.GetCurrentEval()}\nPossible next turns (press a letter to explore deeper, any other key to step back):");

            Stack<GameState> stateHistory = new Stack<GameState>();
            List<GameState> exploreStates = new List<GameState>();
            stateHistory.Push(startState);
            while (stateHistory.Count > 0)
            {
                stateHistory.Peek().PushStates(todo);
                exploreStates.Clear();
                char index = 'A';
                while (todo.TryPop(out ToDo nextAction))
                {
                    if (nextAction.finishProcessing)
                        continue;

                    exploreStates.Add(nextAction.state);
                    StateEval stateEval = nextAction.state.GetCurrentEval();
                    Console.WriteLine($"{index}: ({stateEval}) {nextAction.state.ToString()}");
                    index++;
                }
                ConsoleKeyInfo selection = Console.ReadKey();
                int selectionIdx = (int)selection.Key - 'A';
                if (selectionIdx >= 0 && selectionIdx < exploreStates.Count)
                {
                    stateHistory.Push(exploreStates[selectionIdx]);
                }
                else if (stateHistory.Count > 1)
                {
                    stateHistory.Pop();
                }
                else
                {
                    Console.WriteLine($"Invalid");
                }
                Console.WriteLine($"Selected: {stateHistory.Peek().ToString()}\n");
            }
            Console.WriteLine(startState.GetCurrentEval());
        }

        static void PopulateWinStates()
        {
            //Pre-fill all the states where a player can just attack for the win
            GameState alphaStrikeState = new GameState();
            alphaStrikeState.WhoseTurn = 0;
            alphaStrikeState.P1ThoptersTapped = 0;
            alphaStrikeState.P1CityTapped = 0;
            int numWinningTurns = 0;
            // all possible player 1 alpha strike turns
            for (int p1Life = 1; p1Life <= 40; ++p1Life)
            {
                alphaStrikeState.P1Life = (byte)p1Life;
                for (int elixir = 0; elixir < 3; ++elixir)
                {
                    alphaStrikeState.Elixir = (byte)elixir;
                    for (int p1City = 0; p1City <= 0; ++p1City) // could be tapped if it wasn't P1's turn
                    {
                        alphaStrikeState.P1CityTapped = (byte)p1City;
                        for (int p2Thopters = 0; p2Thopters <= 6; ++p2Thopters)
                        {
                            alphaStrikeState.P2Thopters = (byte)p2Thopters;
                            for (int p2ThoptersTapped = 0; p2ThoptersTapped <= p2Thopters; ++p2ThoptersTapped)
                            {
                                alphaStrikeState.P2ThoptersTapped = (byte)p2ThoptersTapped;
                                for (int walkerASize = 0; walkerASize <= 3; ++walkerASize)
                                {
                                    alphaStrikeState.WalkerASize = (byte)walkerASize;
                                    int walkerAMinState = walkerASize == 0 ? 2 : 0;
                                    int walkerAMaxState = walkerASize == 0 ? 4 : 0; // could be tapped i.e. 1 if it wasn't P1's turn
                                    for (int walkerAState = walkerAMinState; walkerAState <= walkerAMaxState; ++walkerAState)
                                    {
                                        alphaStrikeState.WalkerAState = (byte)walkerAState;
                                        for (int walkerBSize = 0; walkerBSize <= 3; ++walkerBSize)
                                        {
                                            alphaStrikeState.WalkerBSize = (byte)walkerBSize;
                                            for (int walkerCSize = 0; walkerCSize <= 6; ++walkerCSize)
                                            {
                                                alphaStrikeState.WalkerCSize = (byte)walkerCSize;
                                                for (int p1Thopters = 0; p1Thopters <= 9; ++p1Thopters)
                                                {
                                                    alphaStrikeState.P1Thopters = (byte)p1Thopters;
                                                    for (int walkerBTapped = 0; walkerBTapped <= 1; ++walkerBTapped)
                                                    {
                                                        alphaStrikeState.WalkerBTapped = (byte)walkerBTapped;
                                                        for (int walkerCTapped = 0; walkerCTapped <= 1; ++walkerCTapped)
                                                        {
                                                            alphaStrikeState.WalkerCTapped = (byte)walkerCTapped;
                                                            if (walkerASize > 0 && (walkerAState == (int)WalkerAStates.Untapped))
                                                            {
                                                                if ((walkerBSize > 0 && walkerBTapped == 0) || (walkerCSize > 0 && walkerCTapped == 0))
                                                                {
                                                                    // Case 1: p2 can block WalkerA with WalkerB or WalkerC, but thopters are enough to kill
                                                                    int maxDamage = p1Thopters - (p2Thopters - p2ThoptersTapped);
                                                                    for (int p2Life = 1; p2Life <= maxDamage; ++p2Life)
                                                                    {
                                                                        alphaStrikeState.P2Life = (byte)p2Life;
                                                                        alphaStrikeState.InitSetCurrentEval(StateEval.P1Win);
                                                                        numWinningTurns++;
                                                                    }
                                                                }
                                                                else if (p2Thopters == p2ThoptersTapped)
                                                                {
                                                                    // Case 2: p2 has no blockers at all, and walkerA + thopters are large enough to kill
                                                                    int maxDamage = walkerASize + p1Thopters;
                                                                    for (int p2Life = 1; p2Life <= maxDamage; ++p2Life)
                                                                    {
                                                                        alphaStrikeState.P2Life = (byte)p2Life;
                                                                        alphaStrikeState.InitSetCurrentEval(StateEval.P1Win);
                                                                        numWinningTurns++;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    // Case 3: p2 can block WalkerA with a thopter, but remaining thopters are enough to kill
                                                                    int maxDamage = p1Thopters - (p2Thopters - (p2ThoptersTapped + 1));
                                                                    for (int p2Life = 1; p2Life <= maxDamage; ++p2Life)
                                                                    {
                                                                        alphaStrikeState.P2Life = (byte)p2Life;
                                                                        alphaStrikeState.InitSetCurrentEval(StateEval.P1Win);
                                                                        numWinningTurns++;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                // Case 4: walkerA is off the battlefield, but thopters are enough to kill
                                                                int maxDamage = p1Thopters - (p2Thopters - p2ThoptersTapped);
                                                                for (int p2Life = 1; p2Life <= maxDamage; ++p2Life)
                                                                {
                                                                    alphaStrikeState.P2Life = (byte)p2Life;
                                                                    alphaStrikeState.InitSetCurrentEval(StateEval.P1Win);
                                                                    numWinningTurns++;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            numWinningTurns = 0;
            alphaStrikeState.WhoseTurn = 1;
            alphaStrikeState.WalkerBTapped = 0;
            alphaStrikeState.WalkerCTapped = 0;
            for (int p2Life = 1; p2Life <= 20; ++p2Life)
            {
                alphaStrikeState.P2Life = (byte)p2Life;
                for (int elixir = 0; elixir < 3; ++elixir)
                {
                    alphaStrikeState.Elixir = (byte)elixir;
                    for (int p1City = 0; p1City <= 1; ++p1City)
                    {
                        alphaStrikeState.P1CityTapped = (byte)p1City;
                        int elixirLifeBoost = (p1City == 0 && elixir == (int)ElixirStates.Play) ? 5 : 0;
                        for (int p2Thopters = 0; p2Thopters <= 6; ++p2Thopters)
                        {
                            alphaStrikeState.P2Thopters = (byte)p2Thopters;
                            for (int p1Thopters = 0; p1Thopters <= 9; ++p1Thopters)
                            {
                                alphaStrikeState.P1Thopters = (byte)p1Thopters;
                                for (int p1ThoptersTapped = 0; p1ThoptersTapped <= p1Thopters; ++p1ThoptersTapped)
                                {
                                    alphaStrikeState.P1ThoptersTapped = (byte)p1ThoptersTapped;
                                    for (int walkerASize = 0; walkerASize <= 3; ++walkerASize)
                                    {
                                        alphaStrikeState.WalkerASize = (byte)walkerASize;
                                        int walkerAMinState = walkerASize == 0 ? 2 : 0;
                                        int walkerAMaxState = walkerASize == 0 ? 4 : 1;
                                        for (int walkerAState = walkerAMinState; walkerAState <= walkerAMaxState; ++walkerAState)
                                        {
                                            alphaStrikeState.WalkerAState = (byte)walkerAState;
                                            for (int walkerBSize = 0; walkerBSize <= 3; ++walkerBSize)
                                            {
                                                alphaStrikeState.WalkerBSize = (byte)walkerBSize;
                                                for (int walkerCSize = 0; walkerCSize <= 6; ++walkerCSize)
                                                {
                                                    alphaStrikeState.WalkerCSize = (byte)walkerCSize;
                                                    if (walkerBSize > 0) // assume that walkerC is also alive
                                                    {
                                                        if (walkerASize > 0 && (WalkerAStates)walkerAState == WalkerAStates.Untapped)
                                                        {
                                                            if (p1Thopters > p1ThoptersTapped)
                                                            {
                                                                // Case 1: p1 can block WalkerB and WalkerC with WalkerA+thopter, but thopters are enough to kill
                                                                int maxDamage = p2Thopters - (elixirLifeBoost + (p1Thopters - 1) - p1ThoptersTapped);
                                                                for (int p1Life = 1; p1Life <= maxDamage; ++p1Life)
                                                                {
                                                                    alphaStrikeState.P1Life = (byte)p1Life;
                                                                    alphaStrikeState.InitSetCurrentEval(StateEval.P2Win);
                                                                    numWinningTurns++;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                // Case 2: p1 can block WalkerC with WalkerA, but walkerB + thopters are large enough to kill
                                                                int maxDamage = walkerBSize + p2Thopters - elixirLifeBoost;
                                                                for (int p1Life = 1; p1Life <= maxDamage; ++p1Life)
                                                                {
                                                                    alphaStrikeState.P1Life = (byte)p1Life;
                                                                    alphaStrikeState.InitSetCurrentEval(StateEval.P2Win);
                                                                    numWinningTurns++;
                                                                }
                                                            }
                                                        }
                                                        else if (p1Thopters == p1ThoptersTapped)
                                                        {
                                                            // Case 3: p1 has no blockers at all, and walkerB + walkerC + thopters are large enough to kill
                                                            int maxDamage = walkerBSize + walkerCSize + p2Thopters - elixirLifeBoost;
                                                            for (int p1Life = 1; p1Life <= maxDamage; ++p1Life)
                                                            {
                                                                alphaStrikeState.P1Life = (byte)p1Life;
                                                                alphaStrikeState.InitSetCurrentEval(StateEval.P2Win);
                                                                numWinningTurns++;
                                                            }
                                                        }
                                                        else if (p1Thopters == p1ThoptersTapped + 1)
                                                        {
                                                            // Case 4: p1 can block walkerC with a thopter, but walkerB+thopters are large enough to kill
                                                            int maxDamage = walkerBSize + p2Thopters - elixirLifeBoost;
                                                            for (int p1Life = 1; p1Life <= maxDamage; ++p1Life)
                                                            {
                                                                alphaStrikeState.P1Life = (byte)p1Life;
                                                                alphaStrikeState.InitSetCurrentEval(StateEval.P2Win);
                                                                numWinningTurns++;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            // Case 5: p1 can block WalkerB and WalkerC with thopters, but remaining thopters are enough to kill
                                                            int maxDamage = p2Thopters - (elixirLifeBoost + p1Thopters - (p1ThoptersTapped + 2));
                                                            for (int p1Life = 1; p1Life <= maxDamage; ++p1Life)
                                                            {
                                                                alphaStrikeState.P1Life = (byte)p1Life;
                                                                alphaStrikeState.InitSetCurrentEval(StateEval.P2Win);
                                                                numWinningTurns++;
                                                            }
                                                        }
                                                    }
                                                    else if (walkerCSize > 0)
                                                    {
                                                        if (walkerASize > 0 && (WalkerAStates)walkerAState == WalkerAStates.Untapped)
                                                        {
                                                            // Case 6: p1 can block WalkerC with WalkerA, but thopters are enough to kill
                                                            int maxDamage = p2Thopters - (elixirLifeBoost + p1Thopters - p1ThoptersTapped);
                                                            for (int p1Life = 1; p1Life <= maxDamage; ++p1Life)
                                                            {
                                                                alphaStrikeState.P1Life = (byte)p1Life;
                                                                alphaStrikeState.InitSetCurrentEval(StateEval.P2Win);
                                                                numWinningTurns++;
                                                            }
                                                        }
                                                        else if (p1Thopters == p1ThoptersTapped)
                                                        {
                                                            // Case 7: p1 has no blockers at all, and walkerC + thopters are large enough to kill
                                                            int maxDamage = walkerCSize + p2Thopters - elixirLifeBoost;
                                                            for (int p1Life = 1; p1Life <= maxDamage; ++p1Life)
                                                            {
                                                                alphaStrikeState.P1Life = (byte)p1Life;
                                                                alphaStrikeState.InitSetCurrentEval(StateEval.P2Win);
                                                                numWinningTurns++;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            // Case 8: p1 can block WalkerC with a thopter, but remaining thopters are enough to kill
                                                            int maxDamage = p2Thopters - (elixirLifeBoost + p1Thopters - (p1ThoptersTapped + 1));
                                                            for (int p1Life = 1; p1Life <= maxDamage; ++p1Life)
                                                            {
                                                                alphaStrikeState.P1Life = (byte)p1Life;
                                                                alphaStrikeState.InitSetCurrentEval(StateEval.P2Win);
                                                                numWinningTurns++;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // Case 9: no walkers, but thopters are enough to kill
                                                        int maxDamage = p2Thopters - (elixirLifeBoost + p1Thopters - p1ThoptersTapped);
                                                        for (int p1Life = 1; p1Life <= maxDamage; ++p1Life)
                                                        {
                                                            alphaStrikeState.P1Life = (byte)p1Life;
                                                            alphaStrikeState.InitSetCurrentEval(StateEval.P2Win);
                                                            numWinningTurns++;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}