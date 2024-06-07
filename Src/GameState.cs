using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangarbackSim
{
    struct GameState
    {
        /* Packing:
        2x Whose Turn (P1, P2)
        40x P1Life
        20x P2Life
        3x Elixir state (play, deck, hand)
        If P1's turn:
          6x WalkerA state (yard, 1,2,3, deck, hand)
          7x WalkerB state (1,1T,2,2T,3,3T, yard) 0,1,2,3,1T,2T,3T
          13x WalkerC state (1,1T,2,2T,3,3T,4,4T,5,5T,6,6T, yard)
          10x P1Thopters (0-9)
          28x P2Thopters (0,1,1T1,2,2T1,2T2,...6,6T1,6T2,6T3,6T4,6T5,6T6)
        If P2's turn:
          9x WalkerA state (yard, 1,2,3, 1T,2T,3T, deck, hand)
          4x WalkerB state (yard, 1,2,3)
          7x WalkerC state (yard, 1,2,3,4,5,6)
          55x P1Thopters (0, 1,1T, 2,2(1T),2T, 3,3(1T),3(2T),3T, etc..9)
          7x P2Thopters (0-6)
          2x P1City tapped
        = max 291060, divide by 4 because we can fit 4 in a byte = 72765 entries
        total size for results = 375mb
        */
        public static byte[,,,,] results = new byte[2, 41, 21, 3, 72765];

        public byte WhoseTurn; public byte P1Life; public byte P2Life; public byte Elixir;
        public byte WalkerASize;/*0-3*/
        public byte WalkerAState;
        public byte WalkerBSize;/*0-3*/ public byte WalkerBTapped;
        public byte WalkerCSize;/*0-6*/ public byte WalkerCTapped;
        public byte P1CityTapped;
        public byte P1Thopters;/*0-9*/ public byte P1ThoptersTapped;
        public byte P2Thopters;/*0-6*/ public byte P2ThoptersTapped;

        public static int NumStatesCompleted = 0;

        public int GetPackedState()
        {
            int packedState = 0;
            int multiplier = 1;
            if (WhoseTurn == 0)
            {
                switch ((WalkerAStates)WalkerAState)
                {
                    default:
                        //case WalkerAStates.Untapped:
                        //case WalkerAStates.Yard:
                        //case WalkerAStates.Tapped: (should be impossible)
                        packedState += WalkerASize;
                        break;
                    case WalkerAStates.Deck:
                        packedState += 4;
                        break;
                    case WalkerAStates.Hand:
                        packedState += 5;
                        break;
                }
                multiplier *= 6;
                packedState += (WalkerBSize + (WalkerBTapped * 3)) * multiplier;
                multiplier *= 7;
                packedState += (WalkerCSize + (WalkerCTapped * 6)) * multiplier;
                multiplier *= 13;
                packedState += P1Thopters * multiplier;
                multiplier *= 10;
                packedState += TriEncode6(P2Thopters, P2ThoptersTapped) * multiplier;
            }
            else
            {
                switch ((WalkerAStates)WalkerAState)
                {
                    default:
                        //case WalkerAStates.Untapped:
                        //case WalkerAStates.Yard:
                        packedState += WalkerASize;//0-3
                        break;
                    case WalkerAStates.Tapped:
                        packedState += WalkerASize + 3;//4-6
                        break;
                    case WalkerAStates.Deck:
                        packedState += 7;
                        break;
                    case WalkerAStates.Hand:
                        packedState += 8;
                        break;
                }
                multiplier *= 9;
                packedState += WalkerBSize * multiplier;
                multiplier *= 4; //71
                packedState += WalkerCSize * multiplier;
                multiplier *= 7; //497
                packedState += TriEncode9(P1Thopters, P1ThoptersTapped) * multiplier;
                multiplier *= 55; //27335
                packedState += P2Thopters * multiplier;
                multiplier *= 7; //191345
                packedState += P1CityTapped * multiplier;
            }
            return packedState;
        }

        public void UnpackState(int packed)
        {
            if (WhoseTurn == 0)
            {
                int walkerAPacked = packed % 6;
                switch (walkerAPacked)
                {
                    case 0:
                        WalkerASize = 0;
                        WalkerAState = (byte)WalkerAStates.Yard;
                        break;
                    case 1:
                    case 2:
                    case 3:
                        WalkerASize = (byte)walkerAPacked;
                        WalkerAState = (byte)WalkerAStates.Untapped;
                        break;
                    case 4:
                        WalkerASize = 0;
                        WalkerAState = (byte)WalkerAStates.Deck;
                        break;
                    case 5:
                        WalkerASize = 0;
                        WalkerAState = (byte)WalkerAStates.Hand;
                        break;
                }
                packed /= 6;
                int walkerBPacked = packed % 7;
                if (walkerBPacked > 3)
                {
                    WalkerBSize = (byte)(walkerBPacked - 3);
                    WalkerBTapped = 1;
                }
                else
                {
                    WalkerBSize = (byte)walkerBPacked;
                    WalkerBTapped = 0;
                }
                packed /= 7;
                int walkerCPacked = packed % 13;
                if (walkerCPacked > 6)
                {
                    WalkerCSize = (byte)(walkerCPacked - 6);
                    WalkerCTapped = 1;
                }
                else
                {
                    WalkerCSize = (byte)walkerCPacked;
                    WalkerCTapped = 0;
                }
                packed /= 13;
                int p1ThoptersPacked = packed % 10;
                P1Thopters = (byte)p1ThoptersPacked;
                P1ThoptersTapped = 0;
                packed /= 10;
                byte p2ThoptersPacked = (byte)packed;
                TriDecode6(p2ThoptersPacked, out P2Thopters, out P2ThoptersTapped);
            }
            else
            {
                int walkerAPacked = packed % 9;
                switch (walkerAPacked)
                {
                    case 0:
                        WalkerASize = 0;
                        WalkerAState = (byte)WalkerAStates.Yard;
                        break;
                    case 1:
                    case 2:
                    case 3:
                        WalkerASize = (byte)walkerAPacked;
                        WalkerAState = (byte)WalkerAStates.Untapped;
                        break;
                    case 4:
                    case 5:
                    case 6:
                        WalkerASize = (byte)(walkerAPacked - 3);
                        WalkerAState = (byte)WalkerAStates.Tapped;
                        break;
                    case 7:
                        WalkerASize = 0;
                        WalkerAState = (byte)WalkerAStates.Deck;
                        break;
                    case 8:
                        WalkerASize = 0;
                        WalkerAState = (byte)WalkerAStates.Hand;
                        break;
                }
                packed /= 9;
                WalkerBSize = (byte)(packed % 4);
                WalkerBTapped = 0;
                packed /= 4;
                WalkerCSize = (byte)(packed % 7);
                WalkerCTapped = 0;
                packed /= 7;
                byte p1ThoptersPacked = (byte)(packed % 55);
                TriDecode9(p1ThoptersPacked, out P1Thopters, out P1ThoptersTapped);
                packed /= 55;
                P2Thopters = (byte)(packed % 7);
                P2ThoptersTapped = 0;
                packed /= 7;
                P1CityTapped = (byte)packed;
            }
        }

        public static byte TriEncode9(byte num, byte tapped)
        {
            /*  01234
                91234
                98234
                98734
                98764
                98765
                98765
                98765
                98765
                98765
                98765
                = 55 states (0-54)*/

            int x;
            int y;
            if (num <= 4)
            {
                x = num;
                y = tapped;
            }
            else
            {
                x = 9 - num;
                y = 10 - tapped;
            }
            return (byte)(x + y * 5);
        }

        public static void TriDecode9(byte coded, out byte num, out byte tapped)
        {
            byte x = (byte)(coded % 5);
            byte y = (byte)(coded / 5);
            if (x >= y)
            {
                num = x;
                tapped = y;
            }
            else
            {
                num = (byte)(9 - x);
                tapped = (byte)(10 - y);
            }
        }

        public static byte TriEncode6(byte num, byte tapped)
        {
            /*  6012
                6512
                6542
                6543
                6543
                6543
                6543
                = 28 states (0-27)*/

            int x;
            int y;
            if (num <= 2)
            {
                x = num + 1;
                y = tapped;
            }
            else
            {
                x = 6 - num;
                y = 6 - tapped;
            }
            return (byte)(x + y * 4);
        }

        public static void TriDecode6(byte coded, out byte num, out byte tapped)
        {
            byte x = (byte)(coded % 4);
            byte y = (byte)(coded / 4);
            if (x > y)
            {
                num = (byte)(x - 1);
                tapped = y;
            }
            else
            {
                num = (byte)(6 - x);
                tapped = (byte)(6 - y);
            }
        }
        public string ToString()
        {
            string walkerAState;
            switch ((WalkerAStates)WalkerAState)
            {
                case WalkerAStates.Untapped: walkerAState = $"{WalkerASize}/{WalkerASize}"; break;
                case WalkerAStates.Tapped: walkerAState = $"{WalkerASize}/{WalkerASize}T"; break;
                case WalkerAStates.Yard: walkerAState = "Yard"; break;
                case WalkerAStates.Deck: walkerAState = "Deck"; break;
                case WalkerAStates.Hand: walkerAState = "Hand"; break;
                default: walkerAState = "?"; break;
            }
            string walkerBState = WalkerBSize == 0 ? "Yard" : WalkerBTapped == 1 ? $"{WalkerBSize}/{WalkerBSize}T" : $"{WalkerBSize}/{WalkerBSize}";
            string walkerCState = WalkerCSize == 0 ? "Yard" : WalkerCTapped == 1 ? $"{WalkerCSize}/{WalkerCSize}T" : $"{WalkerCSize}/{WalkerCSize}";
            return $"P{WhoseTurn + 1} | {P1Life}:{P2Life} | A:{walkerAState}, E:{(ElixirStates)Elixir}{(P1CityTapped == 1 ? ", City:T" : "")} | B:{walkerBState}, C:{walkerCState} | Th:{P1Thopters}{(P1ThoptersTapped > 0 ? $"({P1ThoptersTapped}T)" : "")}:{P2Thopters}{(P2ThoptersTapped > 0 ? $"({P2ThoptersTapped}T)" : "")}";
        }

        public StateEval GetCurrentEval()
        {
            int packed = GetPackedState();
            int packedShift = ((packed % 4) * 2);
            int packedIndex = packed / 4;

            byte resultBlock = results[WhoseTurn, P1Life, P2Life, Elixir, packedIndex];
            return (StateEval)((resultBlock >> packedShift) & 3);
        }

        public void InitSetCurrentEval(StateEval eval)
        {
            int packed = GetPackedState();
            int packedShift = ((packed % 4) * 2);
            int packedIndex = packed / 4;
            byte resultBlock = results[WhoseTurn, P1Life, P2Life, Elixir, packedIndex];
            resultBlock &= (byte)~(3 << packedShift);
            resultBlock |= (byte)((int)eval << packedShift);
            results[WhoseTurn, P1Life, P2Life, Elixir, packedIndex] = resultBlock;
            NumStatesCompleted++;
        }

        public void SetCurrentEval(StateEval eval)
        {
            int packed = GetPackedState();
            int packedShift = ((packed % 4) * 2);
            int packedIndex = packed / 4;
            byte resultBlock = results[WhoseTurn, P1Life, P2Life, Elixir, packedIndex];
            resultBlock &= (byte)~(3 << packedShift);
            resultBlock |= (byte)((int)eval << packedShift);
            results[WhoseTurn, P1Life, P2Life, Elixir, packedIndex] = resultBlock;
        }

        public void PushStates(Stack<ToDo> todo)
        {
            if (WhoseTurn == 0)
                ResolveP1Turn(todo, true);
            else
                ResolveP2Turn(todo, true);
        }

        public StateEval TryResolve(Stack<ToDo> todo, bool finishProcessing = false)
        {
            StateEval current = GetCurrentEval();

            switch (current)
            {
                case StateEval.P1Win:
                case StateEval.P2Win:
                    return current;

                case StateEval.Draw:
                    if (!finishProcessing)
                        return StateEval.Draw; // if this state is already processing and we got back to it, it's an infinite loop = draw

                    break;
            }

            if (!finishProcessing)
            {
                // ok, we're going to simulate this state, find all child states and request them to be processed.
                // After all that's done, try processing this one again
                todo.Push(new ToDo { state = this, finishProcessing = true });
                SetCurrentEval(StateEval.Draw);
            }

            GameState turnState = this;
            StateEval result = (WhoseTurn == 0) ? turnState.ResolveP1Turn(todo, false) : turnState.ResolveP2Turn(todo, false);
            if (finishProcessing)
            {
                SetCurrentEval(result);
                NumStatesCompleted++;
            }
            return result;
        }

        StateEval ResolveP1Turn(Stack<ToDo> todo, bool forcePushStates)
        {
            // Draw
            if ((WalkerAStates)WalkerAState == WalkerAStates.Deck)
            {
                if ((ElixirStates)Elixir == ElixirStates.Deck)
                {
                    Elixir = (byte)ElixirStates.Hand;
                }
                else
                {
                    WalkerAState = (byte)WalkerAStates.Hand;
                }
            }
            else if ((ElixirStates)Elixir == ElixirStates.Deck)
            {
                Elixir = (byte)ElixirStates.Hand;
            }

            switch ((WalkerAStates)WalkerAState)
            {
                case WalkerAStates.Hand:
                    //must play walker A -> walkerA 1
                    P1CityTapped = 1;
                    WalkerASize = 1;
                    WalkerAState = (byte)WalkerAStates.Untapped;
                    return ResolveP1Combat(todo, forcePushStates, walkerASick: true);
                case WalkerAStates.Yard:
                case WalkerAStates.Deck:
                    switch ((ElixirStates)Elixir)
                    {
                        case ElixirStates.Play:
                            {
                                //must use elixir in upkeep -> P1city tapped, elixir in hand, walkerA in deck
                                P1CityTapped = 1;
                                P1Life = (byte)Math.Clamp(P1Life + 5, 0, 41);
                                WalkerAState = (byte)WalkerAStates.Deck;
                                Elixir = (byte)ElixirStates.Hand;
                                return ResolveP1Combat(todo, forcePushStates);
                            }
                        case ElixirStates.Hand:
                            {
                                //must play elixir->P1city tapped, elixir in play
                                P1CityTapped = 1;
                                Elixir = (byte)ElixirStates.Play;
                                return ResolveP1Combat(todo, forcePushStates);
                            }
                        case ElixirStates.Deck: // I don't think we can get here - if elixir was in your deck, you should have just drawn it
                            {
                                return ResolveP1Combat(todo, forcePushStates);
                            }
                    }
                    break;
                case WalkerAStates.Untapped:
                    if ((ElixirStates)Elixir == ElixirStates.Hand)
                    {
                        //play it and pump walker A->P1city tapped, Walker A tapped & size + 1, elixir in play
                        StateEval result = StateEval.P2Win;
                        if (WalkerASize < 3)
                        {
                            GameState chosenState = this;
                            chosenState.P1CityTapped = 1;
                            chosenState.Elixir = (byte)ElixirStates.Play;
                            chosenState.WalkerASize++;
                            chosenState.WalkerAState = (byte)WalkerAStates.Tapped;
                            P1Choice(ref result, chosenState.ResolveP1Combat(todo, forcePushStates));
                        }
                        //just play it
                        {
                            GameState chosenState = this;
                            chosenState.P1CityTapped = 1;
                            chosenState.Elixir = (byte)ElixirStates.Play;
                            P1Choice(ref result, chosenState.ResolveP1Combat(todo, forcePushStates));
                        }
                        // do neither (planning to block+pump later)
                        {
                            P1Choice(ref result, ResolveP1Combat(todo, forcePushStates));
                        }
                        return result;
                    }
                    else
                    {
                        return ResolveP1Combat(todo, forcePushStates);
                    }
            }
            return StateEval.Unknown;
        }

        StateEval ResolveP1Combat(Stack<ToDo> todo, bool forcePushStates, bool walkerASick = false)
        {
            byte walkerBNewSize = (byte)((WalkerBTapped == 0 && WalkerBSize > 0 && WalkerBSize < WalkerASize) ? WalkerBSize + 1 : WalkerBSize);
            byte walkerCNewSize = (byte)((WalkerCTapped == 0 && WalkerCSize > 0 && WalkerCSize < 6) ? WalkerCSize + 1 : WalkerCSize);
            StateEval p1Choice = StateEval.P2Win;

            if ((WalkerAStates)WalkerAState == WalkerAStates.Untapped && !walkerASick)
            {
                // attacking with WalkerA and N thopters...
                for (int numAttackingThopters = 0; numAttackingThopters <= P1Thopters; numAttackingThopters++)
                {
                    StateEval p2Choice = StateEval.P1Win;
                    int maxBlockingThopters = Math.Min(numAttackingThopters, P2Thopters - P2ThoptersTapped);
                    for (int numBlockingThopters = 0; numBlockingThopters <= maxBlockingThopters; numBlockingThopters++)
                    {
                        byte baseP2Life = (byte)(Math.Max(P2Life - (numAttackingThopters - numBlockingThopters), 0));
                        byte baseP1Thopters = (byte)(P1Thopters - numBlockingThopters);
                        byte baseP1ThoptersTapped = (byte)(numAttackingThopters - numBlockingThopters);
                        byte baseP2Thopters = (byte)(P2Thopters - numBlockingThopters);

                        if (WalkerBSize > 0 && WalkerBTapped == 0 && (WalkerBSize == WalkerASize || WalkerBSize == WalkerASize - 1))
                        {
                            // trading WalkerA with WalkerB
                            GameState postCombatState = this;
                            postCombatState.WalkerBSize = 0;
                            postCombatState.WalkerASize = 0;
                            postCombatState.WalkerAState = (byte)WalkerAStates.Yard;
                            postCombatState.P2Life = baseP2Life;
                            postCombatState.P1Thopters = (byte)Math.Clamp(baseP1Thopters + WalkerASize, 0, 9);
                            postCombatState.P1ThoptersTapped = baseP1ThoptersTapped;
                            postCombatState.P2Thopters = (byte)Math.Clamp(baseP2Thopters + WalkerASize, 0, 6);
                            postCombatState.WalkerCSize = walkerCNewSize;
                            P2Choice(ref p2Choice, postCombatState.ResolveP1EndOfTurn(todo, forcePushStates));
                        }
                        if (WalkerCTapped == 0 && WalkerCSize > WalkerASize)
                        {
                            // blocking WalkerA with WalkerC
                            GameState postCombatState = this;
                            postCombatState.P2Life = baseP2Life;
                            postCombatState.WalkerASize = 0;
                            postCombatState.WalkerAState = (byte)WalkerAStates.Yard;
                            postCombatState.P1Thopters = (byte)Math.Clamp(baseP1Thopters + WalkerASize, 0, 9);
                            postCombatState.P1ThoptersTapped = baseP1ThoptersTapped;
                            postCombatState.P2Thopters = baseP2Thopters;
                            postCombatState.WalkerBSize = walkerBNewSize;
                            postCombatState.WalkerCSize = walkerCNewSize;
                            P2Choice(ref p2Choice, postCombatState.ResolveP1EndOfTurn(todo, forcePushStates));
                        }
                        if (numBlockingThopters < (P2Thopters - P2ThoptersTapped))
                        {
                            // blocking WalkerA with an extra thopter
                            GameState postCombatState = this;
                            postCombatState.P2Life = baseP2Life;
                            postCombatState.P1Thopters = baseP1Thopters;
                            postCombatState.P1ThoptersTapped = baseP1ThoptersTapped;
                            postCombatState.P2Thopters = (byte)(baseP2Thopters - 1);
                            postCombatState.WalkerAState = (byte)WalkerAStates.Tapped;
                            postCombatState.WalkerBSize = walkerBNewSize;
                            postCombatState.WalkerCSize = walkerCNewSize;
                            P2Choice(ref p2Choice, postCombatState.ResolveP1EndOfTurn(todo, forcePushStates));
                        }
                        {
                            // letting WalkerA through
                            GameState postCombatState = this;
                            postCombatState.P2Life = (byte)Math.Max(baseP2Life - WalkerASize, 0);
                            postCombatState.P1Thopters = baseP1Thopters;
                            postCombatState.P1ThoptersTapped = baseP1ThoptersTapped;
                            postCombatState.P2Thopters = baseP2Thopters;
                            postCombatState.WalkerAState = (byte)WalkerAStates.Tapped;
                            postCombatState.WalkerBSize = walkerBNewSize;
                            postCombatState.WalkerCSize = walkerCNewSize;
                            P2Choice(ref p2Choice, postCombatState.ResolveP1EndOfTurn(todo, forcePushStates));
                        }
                    }
                    P1Choice(ref p1Choice, p2Choice);
                }
            }

            // attacking only with N thopters...
            for (int numAttackingThopters = 0; numAttackingThopters <= P1Thopters; numAttackingThopters++)
            {
                StateEval p2Choice = StateEval.P1Win;
                int maxBlockingThopters = Math.Min(numAttackingThopters, P2Thopters - P2ThoptersTapped);
                for (int numBlockingThopters = 0; numBlockingThopters <= maxBlockingThopters; numBlockingThopters++)
                {
                    GameState postCombatState = this;
                    postCombatState.P2Life = (byte)Math.Max(postCombatState.P2Life - (numAttackingThopters - numBlockingThopters), 0);
                    postCombatState.P1Thopters = (byte)(postCombatState.P1Thopters - numBlockingThopters);
                    postCombatState.P1ThoptersTapped = (byte)(numAttackingThopters - numBlockingThopters);
                    postCombatState.P2Thopters = (byte)(postCombatState.P2Thopters - numBlockingThopters);
                    postCombatState.WalkerBSize = walkerBNewSize;
                    postCombatState.WalkerCSize = walkerCNewSize;
                    P2Choice(ref p2Choice, postCombatState.ResolveP1EndOfTurn(todo, forcePushStates));
                }
                P1Choice(ref p1Choice, p2Choice);
            }

            return p1Choice;
        }

        StateEval ResolveP1EndOfTurn(Stack<ToDo> todo, bool forcePushStates)
        {
            if (P1Life == 0)
            {
                if (forcePushStates)
                    todo.Push(new ToDo { state = this });
                return StateEval.P2Win;
            }
            else if (P2Life == 0)
            {
                if (forcePushStates)
                    todo.Push(new ToDo { state = this });
                return StateEval.P1Win;
            }
            else if (P1Life > 40) // assume this means we have an infinite life loop
            {
                if (forcePushStates)
                    todo.Push(new ToDo { state = this });
                return StateEval.P1Win;
            }

            GameState stateToRequest = this;
            stateToRequest.WhoseTurn = 1;
            stateToRequest.WalkerBTapped = 0;
            stateToRequest.WalkerCTapped = 0;
            stateToRequest.P2ThoptersTapped = 0;
            StateEval eval = stateToRequest.GetCurrentEval();
            if (eval == StateEval.Unknown || forcePushStates)
            {
                todo.Push(new ToDo { state = stateToRequest });
            }
            return eval;
        }

        StateEval ResolveP2Turn(Stack<ToDo> todo, bool forcePushStates)
        {
            return ResolveP2Combat(todo, forcePushStates);
        }

        enum P1BlockType
        {
            None,
            Thopter,
            WalkerA
        }

        StateEval ResolveP2Combat(Stack<ToDo> todo, bool forcePushStates)
        {
            byte walkerANewSize = (byte)((WalkerAState == (byte)WalkerAStates.Untapped && WalkerASize > 0 && WalkerASize < 3) ? WalkerASize + 1 : WalkerASize);
            StateEval p2Choice = StateEval.P1Win;
            bool walkerACanBlock = WalkerAState == (byte)WalkerAStates.Untapped;
            int P1UntappedThopters = P1Thopters - P1ThoptersTapped;
            P1BlockType maxBlockType = walkerACanBlock ? P1BlockType.WalkerA : (P1UntappedThopters > 0 ? P1BlockType.Thopter : P1BlockType.None);
            int walkerBCanAttack = (WalkerBTapped == 0 && WalkerBSize > 0) ? 1 : 0;
            int walkerCCanAttack = (WalkerCTapped == 0 && WalkerCSize > 0) ? 1 : 0;

            for (int numAttackingThopters = 0; numAttackingThopters <= P2Thopters; numAttackingThopters++)
            {
                for (int walkerBAttacks = 0; walkerBAttacks <= walkerBCanAttack; ++walkerBAttacks)
                {
                    int walkerBMaxBlockType = (int)(walkerBAttacks == 1 ? maxBlockType : P1BlockType.None);
                    for (int walkerCAttacks = 0; walkerCAttacks <= walkerCCanAttack; ++walkerCAttacks)
                    {
                        StateEval p1Choice = StateEval.P2Win;
                        int walkerACanPump = (WalkerASize < 3 && WalkerAState == (byte)WalkerAStates.Untapped && P1CityTapped == 0) ? 1 : 0;
                        for (int walkerAPumps = 0; walkerAPumps <= walkerACanPump; ++walkerAPumps)
                        {
                            int walkerACurrentSize = WalkerASize + walkerAPumps;

                            int walkerCMaxBlockType = (int)(walkerCAttacks == 1 ? maxBlockType : P1BlockType.None);
                            for (int walkerBBlockType = 0; walkerBBlockType <= walkerBMaxBlockType; ++walkerBBlockType)
                            {
                                if (walkerBBlockType == (int)P1BlockType.Thopter && P1UntappedThopters == 0)
                                    continue;

                                for (int walkerCBlockType = 0; walkerCBlockType <= walkerCMaxBlockType; ++walkerCBlockType)
                                {
                                    // walkerA can't double block
                                    if (walkerBBlockType == (int)P1BlockType.WalkerA && walkerCBlockType == (int)P1BlockType.WalkerA)
                                        continue;

                                    int remainingUntappedThopters = P1UntappedThopters;
                                    if (walkerBBlockType == (int)P1BlockType.Thopter)
                                        remainingUntappedThopters--;
                                    if (walkerCBlockType == (int)P1BlockType.Thopter)
                                        remainingUntappedThopters--;
                                    if (remainingUntappedThopters < 0)
                                        continue;

                                    GameState postCombatState = this;
                                    postCombatState.WalkerASize = (byte)(WalkerASize + walkerAPumps);
                                    postCombatState.WalkerBTapped = (byte)walkerBAttacks;
                                    postCombatState.WalkerCTapped = (byte)walkerCAttacks;
                                    if (walkerAPumps == 1)
                                    {
                                        postCombatState.P1CityTapped = 1;
                                        postCombatState.WalkerAState = (byte)WalkerAStates.Tapped;
                                    }
                                    int finalP1Life = P1Life - numAttackingThopters;
                                    int finalP1Thopters = P1Thopters;
                                    int finalP2Thopters = P2Thopters;
                                    int finalP2ThoptersTapped = numAttackingThopters;

                                    if (walkerBAttacks == 1)
                                    {
                                        switch ((P1BlockType)walkerBBlockType)
                                        {
                                            case P1BlockType.None:
                                                finalP1Life -= WalkerBSize;
                                                break;
                                            case P1BlockType.Thopter:
                                                finalP1Thopters--;
                                                break;
                                            case P1BlockType.WalkerA:
                                                if (walkerACurrentSize <= WalkerBSize)
                                                {
                                                    finalP1Thopters = Math.Clamp(finalP1Thopters + walkerACurrentSize, 0, 9);
                                                    postCombatState.WalkerASize = 0;
                                                    postCombatState.WalkerAState = (byte)WalkerAStates.Yard;
                                                }
                                                if (WalkerBSize <= walkerACurrentSize)
                                                {
                                                    finalP2Thopters = Math.Clamp(finalP2Thopters + WalkerBSize, 0, 6);
                                                    postCombatState.WalkerBSize = 0;
                                                    postCombatState.WalkerBTapped = 0;
                                                }
                                                break;
                                        }
                                    }
                                    if (walkerCAttacks == 1)
                                    {
                                        switch ((P1BlockType)walkerCBlockType)
                                        {
                                            case P1BlockType.None:
                                                finalP1Life -= WalkerCSize;
                                                break;
                                            case P1BlockType.Thopter:
                                                finalP1Thopters--;
                                                break;
                                            case P1BlockType.WalkerA:
                                                if (walkerACurrentSize <= WalkerCSize)
                                                {
                                                    finalP1Thopters = Math.Clamp(finalP1Thopters + walkerACurrentSize, 0, 9);
                                                    postCombatState.WalkerASize = 0;
                                                    postCombatState.WalkerAState = (byte)WalkerAStates.Yard;
                                                }
                                                if (WalkerCSize <= walkerACurrentSize)
                                                {
                                                    finalP2Thopters = Math.Clamp(finalP2Thopters + WalkerCSize, 0, 6);
                                                    postCombatState.WalkerCSize = 0;
                                                    postCombatState.WalkerCTapped = 0;
                                                }
                                                break;
                                        }
                                    }

                                    int maxBlockingThopters = Math.Min(remainingUntappedThopters, numAttackingThopters);
                                    for (int numBlockingThopters = 0; numBlockingThopters <= maxBlockingThopters; numBlockingThopters++)
                                    {
                                        GameState finalCombatState = postCombatState;
                                        int elixirLifeBoost = 0;
                                        if (finalP1Life + numBlockingThopters < 1)
                                        {
                                            if ((finalP1Life + numBlockingThopters) > -5 && Elixir == (byte)ElixirStates.Play && finalCombatState.P1CityTapped == 0)
                                            {
                                                // must use Elixir before blocking, to not die
                                                elixirLifeBoost = 5;
                                                finalCombatState.Elixir = (byte)ElixirStates.Deck;
                                                finalCombatState.P1CityTapped = 1;
                                                if (WalkerAState == (byte)WalkerAStates.Yard)
                                                    finalCombatState.WalkerAState = (byte)WalkerAStates.Deck;
                                            }
                                            else
                                            {
                                                // must block more, we're dying here
                                                continue;
                                            }
                                        }

                                        finalCombatState.P1Life = (byte)Math.Clamp(finalP1Life + numBlockingThopters + elixirLifeBoost, 0, 41);
                                        finalCombatState.P1Thopters = (byte)(finalP1Thopters - numBlockingThopters);
                                        finalCombatState.P2Thopters = (byte)(finalP2Thopters - numBlockingThopters);
                                        finalCombatState.P2ThoptersTapped = (byte)(finalP2ThoptersTapped - numBlockingThopters);

                                        // should it be optional to use Elixir at end of turn?
                                        if (finalCombatState.Elixir == (byte)ElixirStates.Play && finalCombatState.P1CityTapped == 0)
                                        {
                                            finalCombatState.P1Life = (byte)Math.Clamp(finalCombatState.P1Life + 5, 0, 41);
                                            finalCombatState.Elixir = (byte)ElixirStates.Deck;
                                            finalCombatState.P1CityTapped = 1;
                                            if (finalCombatState.WalkerAState == (byte)WalkerAStates.Yard)
                                                finalCombatState.WalkerAState = (byte)WalkerAStates.Deck;
                                        }

                                        P1Choice(ref p1Choice, finalCombatState.ResolveP2EndOfTurn(todo, forcePushStates));
                                    }
                                }
                            }
                        }
                        P2Choice(ref p2Choice, p1Choice);
                    }
                }
            }
            return p2Choice;
        }

        StateEval ResolveP2EndOfTurn(Stack<ToDo> todo, bool forcePushStates)
        {
            if (P1Life == 0)
            {
                if (forcePushStates)
                    todo.Push(new ToDo { state = this });
                return StateEval.P2Win;
            }
            else if (P2Life == 0)
            {
                if (forcePushStates)
                    todo.Push(new ToDo { state = this });
                return StateEval.P1Win;
            }
            else if (P1Life > 40) // assume this means we have an infinite life loop
            {
                if (forcePushStates)
                    todo.Push(new ToDo { state = this });
                return StateEval.P1Win;
            }

            GameState stateToRequest = this;
            stateToRequest.WhoseTurn = 0;
            if (stateToRequest.WalkerAState == (byte)WalkerAStates.Tapped)
                stateToRequest.WalkerAState = (byte)WalkerAStates.Untapped;
            stateToRequest.P1CityTapped = 0;
            stateToRequest.P1ThoptersTapped = 0;
            StateEval eval = stateToRequest.GetCurrentEval();
            if (eval == StateEval.Unknown || forcePushStates)
            {
                todo.Push(new ToDo { state = stateToRequest });
            }

            return eval;
        }

        // preference order: Unknown, P1Win, Draw, P2Win
        void P1Choice(ref StateEval state, StateEval newOption)
        {
            if (newOption == StateEval.Unknown)
            {
                state = StateEval.Unknown;
                return;
            }

            switch (state)
            {
                case StateEval.Unknown:
                    return;
                case StateEval.P2Win:
                    state = newOption;
                    return;
                case StateEval.P1Win:
                    return;
                case StateEval.Draw:
                    if (newOption == StateEval.P1Win)
                        state = StateEval.P1Win;
                    return;
            }
        }

        // preference order: Unknown, P2Win, Draw, P1Win
        void P2Choice(ref StateEval state, StateEval newOption)
        {
            if (newOption == StateEval.Unknown)
            {
                state = StateEval.Unknown;
                return;
            }

            switch (state)
            {
                case StateEval.Unknown:
                    return;
                case StateEval.P1Win:
                    state = newOption;
                    return;
                case StateEval.P2Win:
                    return;
                case StateEval.Draw:
                    if (newOption == StateEval.P2Win)
                        state = StateEval.P2Win;
                    return;
            }
        }
    }
}
