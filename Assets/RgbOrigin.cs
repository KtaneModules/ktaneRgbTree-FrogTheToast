using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
//using System.Collections.IEnumerable;
using System.Reflection;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using Random = UnityEngine.Random;


public class RgbOrigin : MonoBehaviour {

    public KMAudio Audio;
    public KMBombInfo bombInf;
    public KMBombModule Module;
    public KMSelectable theModule;
    public KMColorblindMode ColorblindMode;

    public AudioSource aud;
    public GameObject buttonTemplate;
    public GameObject buttonParent;

    List<Button> buttonList = new List<Button>();
    List<Node> nodeList = new List<Node>();
    List<Node> leafNodes = new List<Node>();
    List<Node> validTargets = new List<Node>();

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved = false;
    bool colorblindModeEnabled = false;

    Node badNode = new Node(-1);
    Node rootNode;
    Node lastClicked;

    // Use this for initialization
    void Awake() {
        // im not trying to spell exactly
        // exatly

        moduleId = moduleIdCounter++;
        // initialize empty nodes, empty maze, empty colors, empy
        for (int i = 0; i < 36; i++) 
            nodeList.Add(new Node(i));

        // give nodes a sense of direction for once
        badNode.solved = true; badNode.point = badNode;
        for (int i = 0; i < 36; i++) { 
            if (CY(i + 1) == CY(i)) nodeList[i].R = nodeList[i + 1];
            else nodeList[i].R = badNode;
            if (CY(i - 1) == CY(i)) nodeList[i].L = nodeList[i - 1];
            else nodeList[i].L = badNode;
            if (i + 6 <= 35) nodeList[i].U = nodeList[i + 6];
            else nodeList[i].U = badNode;
            if (i - 6 >= 0) nodeList[i].D = nodeList[i - 6];
            else nodeList[i].D = badNode;
        }

        // create a perfect maze
        for (int i = 0; i < 36; i++) { 
            if ((i + 1) % 6 == 0 && i != 35)
                nodeList[i].point = nodeList[i].U;
            else 
                nodeList[i].point = nodeList[i].R;
        } rootNode = nodeList[nodeList.Count - 1]; rootNode.point = badNode;

        // origin shift algorithm. really easy actually
        Node n = badNode; var ran = Enumerable.Range(0, 4).ToList(); 
        for (int i = 0; i < (6 * 6 * 10); i++) {
            ran.Shuffle();
            for (int i1 = 0; i1 < 4; i1++) {
                switch (ran[i1]) {
                    case 0: n = rootNode.L; break;
                    case 1: n = rootNode.U; break;
                    case 2: n = rootNode.R; break;
                    case 3: n = rootNode.D; break;
                } if (n.id != -1) break;
            }
            rootNode.point = n;
            rootNode = n;
            rootNode.point = badNode;
            n = badNode;
        }

        // "sub" is how many neigbour ppont to big neighbour target neignomrua pomt!
        foreach (var node in nodeList) {
            if (node.L.point == node) node.sub++;
            if (node.U.point == node) node.sub++;
            if (node.R.point == node) node.sub++;
            if (node.D.point == node) node.sub++;
            if (node.sub == 0) leafNodes.Add(node);
        }

        List<int> stupid = leafNodes.Select(x => x.id).ToList(); stupid.Sort();
        List<int> yourstupid = new List<int>();

        // find valid targets if you couldnt tell
        validTargets.Add(badNode);
        foreach (var node in nodeList) {
            for (Node n1 = node; n1.point.id != -1; n1 = n1.point)
                node.step++;
            if (validTargets[0].step < node.step) {
                validTargets.Clear(); validTargets.Add(node);
            }
            else if (validTargets[0].step == node.step) {
                validTargets.Add(node);
            }
        }
        string guh = "";
        foreach (var node in validTargets)
            guh += Con(node.id) + " ";
        Debug.LogFormat("[RGB Tree #{0}] Valid targets: {1}", moduleId, guh);

        // yeah
        RegenRgb(); Solver();

        // debugug
        Debug.LogFormat("[RGB Tree #{0}] Generated tree:", moduleId);
        Debug.LogFormat("[RGB Tree #{0}] + a  b  c  d  e  f", moduleId);
        List<string> ble = new List<string>();
        for (int i = 0; i < 6; i++) {
            guh = (6 - i).ToString() + " ";
            for (int i2 = 0; i2 < 6; i2++) {
                n = nodeList[(i * 6) + i2];
                if (rootNode.id == n.id) guh += "o ";
                else {
                    if (n.point.id == n.L.id) guh += "← ";
                    if (n.point.id == n.U.id) guh += "↑  ";
                    if (n.point.id == n.R.id) guh += "→ ";
                    if (n.point.id == n.D.id) guh += "↓  ";
                }
            } ble.Add(guh);
        } ble.Reverse();
        foreach (var item in ble)
            Debug.LogFormat("[RGB Tree #{0}] {1}", moduleId, item);
        if (solvable) Debug.LogFormat("[RGB Tree #{0}] The generated tree CAN INDEED be solved! DO NOT ENTER THE EXTRA MODE!!!", moduleId);
        else { Debug.LogFormat("[RGB Tree #{0}] The generated tree is unsolvable! Extra mode enabled.", moduleId);
            Debug.LogFormat("[RGB Tree #{0}] Ambigous Nodes: {1}", moduleId, GLOB); }

        // 30 31 32 33 34 35
        // 24 25 26 27 28 29
        // 18 19 20 21 22 23
        // 12 13 14 15 16 17
        // 6  7  8  9  10 11
        // 0  1  2  3  4  5

        int id = 0;
        for (float z = -.06f; z < .07f; z += .024f) {
            for (float x = -.06f; x < .07f; x += .024f) { 
                var obj = Instantiate(buttonTemplate, buttonParent.transform);
                var position = obj.transform.localPosition; position.x = x; position.y = buttonParent.transform.localPosition.y; position.z = z;
                obj.transform.localPosition = position;
                obj.name = id.ToString();

                var bt = new Button(obj, id, nodeList[id]); nodeList[id].bt = bt;
                QuickColorChange(bt, Determiner(bt.node));

                bt.sel.OnInteract += delegate () {
                    if (!transgender && !bt.trans && !failure && !moduleSolved) {
                        bt.trans = true;
                        if (!submission && !themode) {
                            transgender = true;
                            // clickin root
                            if (bt.id == rootNode.id) {
                                transgender = false;
                                submission = true; lastClicked = bt.node;
                                StartCoroutine(ColorChange(bt, bt.col, bt.col));
                            }
                            // clickin leaf
                            else if (bt.node.sub == 0) {
                                yourstupid.Add(bt.id);
                                // checkin if your trying to enter the mode. you know. the mode.
                                if (yourstupid.Count == stupid.Count) {
                                    bool no = true;
                                    for (int i = 0; i < stupid.Count; i++)
                                        if (yourstupid[i] != stupid[i])
                                            no = false;
                                    if (!no) {
                                        yourstupid.RemoveAt(0);
                                        RegenRgb();
                                        foreach (var node in nodeList)
                                            StartCoroutine(ColorChange(node.bt, node.bt.col, Determiner(node)));
                                    }
                                    else {
                                        // YOU CAN'T ENTER THE MODE!!!! THE TREE IS SOLVABLE!!! STRIKE FOR YOU AHAHHAHHAUWDGYAWDYHAWYDGWDAYAWYDG
                                        if (solvable) {
                                            failure = true;
                                            Debug.LogFormat("[RGB Tree #{0}] THE TREE IS SOLVABLE!!!!!!!! YOU CAN'T ENTER THAT MODE!!!!!! AAAAAAAAGGGHHHHH!!!!", moduleId);
                                            StartCoroutine(Failure(bt));
                                            GetComponent<KMBombModule>().HandleStrike();
                                        }
                                        // NEW MODE!!!!!!!!!!
                                        else {
                                            themode = true;
                                            var numbers = Enumerable.Range(1, 7).ToList().Shuffle(); numbers.RemoveRange(4, 3);
                                            if (colorblindModeEnabled)
                                                foreach (var node in nodeList)
                                                    validBits.Add(node.cbit);
                                            foreach (var node in nodeList) {
                                                if (node.solved) {
                                                    if (colorblindModeEnabled)
                                                        node.cbit = 0;
                                                    StartCoroutine(ColorChange(node.bt, node.bt.col, Color.black));
                                                }
                                                else {
                                                    int cbit = 0; float num = node.sub == 0 ? .25f : .6f;
                                                    if (node.point.id == -1) cbit = numbers.PickRandom();
                                                    else if (node.point.id == node.L.id) cbit = numbers[0];
                                                    else if (node.point.id == node.U.id) cbit = numbers[1];
                                                    else if (node.point.id == node.R.id) cbit = numbers[2];
                                                    else if (node.point.id == node.D.id) cbit = numbers[3];

                                                    if (colorblindModeEnabled)
                                                        node.cbit = cbit;
                                                    StartCoroutine(ColorChange(node.bt, node.bt.col,
                                                        new Color(
                                                            (cbit & 1) != 0 ? num : 0f,
                                                            (cbit & 2) != 0 ? num : 0f,
                                                            (cbit & 4) != 0 ? num : 0f)));
                                                }
                                            }
                                        }
                                        yourstupid.Clear();
                                    }
                                }
                                else {
                                    RegenRgb();
                                    foreach (var node in nodeList)
                                        StartCoroutine(ColorChange(node.bt, node.bt.col, Determiner(node)));
                                }
                            }
                            // clickin a WRONG NODE DUMMY!!!!!!!!!!!
                            else {
                                failure = true;
                                Debug.LogFormat("[RGB Tree #{0}] NODE {1} IS NOT THE ROOT NODE!!!!!!!!", moduleId, Con(bt.id));
                                StartCoroutine(Failure(bt));
                                GetComponent<KMBombModule>().HandleStrike();
                            }
                        }
                        // submitting answer
                        else if (submission && !themode) {
                            // if on valid path
                            if (bt.node.point == lastClicked || lastClicked.point == bt.node) {
                                lastClicked = bt.node;
                                if (validTargets.Any(v => bt.id == v.id)) {
                                    Debug.LogFormat("[RGB Tree #{0}] YOU FREAKING DID IT!!!!!!!!!", moduleId);
                                    Audio.PlaySoundAtTransform("solve_click", transform);
                                    GetComponent<KMBombModule>().HandlePass(); moduleSolved = true;
                                    StartCoroutine(Solvin());
                                } else StartCoroutine(ColorChange(bt, bt.col, bt.col));
                            }
                            // if NOT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                            else {
                                failure = true; submission = false; bt.trans = false;
                                Debug.LogFormat("[RGB Tree #{0}] NODE {1} IS NOT ON A VALID PATH!!!!!!!!", moduleId, Con(bt.id));
                                StartCoroutine(Failure(bt));
                                GetComponent<KMBombModule>().HandleStrike();
                            }
                        }
                        // exiting da mode
                        else {
                            if (bt.node.sub == 0) {
                                themode = false;
                                if (colorblindModeEnabled) { 
                                    foreach (var node in nodeList)
                                        node.cbit = validBits[node.id];
                                    validBits.Clear(); }
                                foreach (var node in nodeList)
                                    StartCoroutine(ColorChange(node.bt, node.bt.col, Determiner(node)));
                            }
                        }
                        Audio.PlaySoundAtTransform("button_click", transform);
                        StartCoroutine(Push(bt));
                    }
                    return false;
                };

                buttonList.Add(bt);
                id++;
            }
        }

        Destroy(buttonTemplate);
        theModule.Children = buttonList.Select(x => x.sel).ToArray();
        theModule.UpdateChildrenProperly(); //buttonList.RemoveAt(buttonList.Count - 1);

    }

    List<List<int>> valCombos = new List<List<int>>();
    List<Node> ambigousList = new List<Node>();
    List<Node> FilterCands = new List<Node>();
    void FindAllCandidates(List<Node> lis) {
        FilterCands.Clear();
        foreach (var node in lis) {
            if (node.L.id != -1)
                if (!node.L.solved && node.L.sub > 0)
                    adj.Add(node.L);
            if (node.U.id != -1)
                if (!node.U.solved && node.U.sub > 0)
                    adj.Add(node.U);
            if (node.R.id != -1)
                if (!node.R.solved && node.R.sub > 0)
                    adj.Add(node.R);
            if (node.D.id != -1)
                if (!node.D.solved && node.D.sub > 0)
                    adj.Add(node.D);

            foreach (var nbr in adj)
                FilterCands.Add(new Node(node.id) { point = nbr, step = 0 });
            adj.Clear();
        }

        /*string guh = "";
        foreach (var node in FilterCands)
            guh += Con(node.id) + ">" + Con(node.point.id) + ",";
        Debug.Log(guh);
        */
    }
    List<Node> PleaseCloneList(List<Node> lis) {
        List<Node> newLis = new List<Node>();

        foreach (var node in lis) {
            newLis.Add(new Node(node.id) {
                L = node.L,
                U = node.U,
                R = node.R,
                D = node.D,
                point = node.point,
                sub = node.sub,
                cbit = node.cbit,
                solved = node.solved
            });
        }

        foreach (var node in newLis) {
            if (node.L.id > -1) node.L = newLis[node.L.id];
            if (node.U.id > -1) node.U = newLis[node.U.id];
            if (node.R.id > -1) node.R = newLis[node.R.id];
            if (node.D.id > -1) node.D = newLis[node.D.id];
            if (node.point.id != -1) node.point = newLis[node.point.id];
        }

        return newLis;
    }
    void ResetList(List<Node> template, List<Node> current) {
        for (int i = 0; i < template.Count; i++) {
            current[i].solved = template[i].solved;
            if (template[i].point.id == -1)
                current[i].point = badNode;
            else
                current[i].point = current[template[i].point.id];

        }
    }

    List<Node> adj = new List<Node>();
    List<int> validBits = new List<int>();
    void ColorCheck(Node node, ref Node root, ref bool freedom, ref int count) {
        if (!node.solved && node.sub > 0) {
            int xor1 = 0;
            string guh = "";

            // records adj candidates. in-bounds, points to nothing, not pointed to by target.
            // if adj points to target, add it to xor
            if (node.L.id != -1) {
                if (node.L.point.id == -1 && node.point.id != node.L.id) { adj.Add(node.L); guh += "← "; }
                else if (node.L.point.id == node.id) xor1 ^= node.L.cbit;
            }
            if (node.U.id != -1) {
                if (node.U.point.id == -1 && node.point.id != node.U.id) { adj.Add(node.U); guh += "↑  "; }
                else if (node.U.point.id == node.id) xor1 ^= node.U.cbit;
            }
            if (node.R.id != -1) {
                if (node.R.point.id == -1 && node.point.id != node.R.id) { adj.Add(node.R); guh += "→ "; }
                else if (node.R.point.id == node.id) xor1 ^= node.R.cbit;
            }
            if (node.D.id != -1) {
                if (node.D.point.id == -1 && node.point.id != node.D.id) { adj.Add(node.D); guh += "↓  "; }
                else if (node.D.point.id == node.id) xor1 ^= node.D.cbit;
            }

            // remove root obvi
            if (freedom) {
                int r = root.id;
                adj.RemoveAll(x => x.id == r);
            }
            // happens when adj nodes were edited around target
            if (adj.Count == 0) {
                node.solved = true; //Debug.Log(Con(node.id) + " is SOLVED");
            }

            //guh = "";
            //foreach (var nd in adj)
            //    guh += Con(nd.id) + " ";
            //guh = Con(node.id) + ": " + guh;
            //Debug.Log(Con(node.id) + ": " + guh);

            if (!node.solved) {
                int subCount = 1 << adj.Count;
                //Debug.Log(Convert.ToString(xor1, 2).PadLeft(3, '0') + " ===> GET TO: " + Convert.ToString(node.cbit, 2).PadLeft(3, '0'));

                // uses bits to test every combo of node pointin to target node. fancy.
                // please remember ^ does xor not exponent
                for (int subset = 0; subset < subCount; subset++) {
                    int xor = xor1;
                    for (int i = 0; i < adj.Count; i++)
                        if ((subset & (1 << i)) != 0) // which adj's cbit is being added
                            xor ^= adj[i].cbit;
                    //Debug.Log(Convert.ToString(xor, 2).PadLeft(3, '0') + " = " + Convert.ToString(subset, 2).PadLeft(adj.Count, '0') + " = " + subset.ToString());
                    if (node.cbit == xor)
                        validBits.Add(subset);
                }/*
                guh = Con(node.id) + ",VALID: " + validBits.Count + "   "; string gor = "";
                foreach (var bit in validBits) {
                    guh += bit.ToString() + "-";
                    gor = Convert.ToString(bit, 2).PadLeft(adj.Count, '0'); //gor.Reverse();
                    guh += gor + " ";
                } Debug.Log(guh);
                */
                for (int i = 0; i < adj.Count; i++) {
                    bool ubiquitously = true;
                    foreach (int bit in validBits) {
                        if ((bit & (1 << i)) == 0) {
                            ubiquitously = false;
                            break;
                        }
                    }
                    if (ubiquitously) { // if an adj appears in every valid combo
                        //Debug.Log(Con(adj[i].id) + " poitns to " + Con(node.id));
                        adj[i].point = node;
                    }
                }
                if (validBits.Count == 1) { // if only one combo can solve the target, solve
                    node.solved = true; //Debug.Log(Con(node.id) + " is SOLVED");
                }
            }
        }
        // determine if target is the root outside of (solved and unsolved)
        if (!freedom && node.point.id == -1 && node.sub > 0) {
            node.step = 0;
            // out of bounds                         skip
            // in bounds, unsolved, unpointed (-1)      deny
            // in bounds, SOLVED, unpointed (-1)        YES
            // in bounds, unsolved, POINTED (target)    YES
            // in bounds, unsolved, POINTED (non)       deny
            // in bounds, SOLVED, POINTED (target)      YES
            // in bounds, SOLVED, POINTED (non)         YES

            if (node.L.id != -1 && node.L.sub > 0) // skip if adj is OUT of bounds or LEAF
                if (!node.L.solved) // if adj points away from target
                    if (node.L.point.id != node.id) // and if adj is unsolved
                        node.step++;
            if (node.U.id != -1 && node.U.sub > 0)
                if (node.U.point.id != node.id)
                    if (!node.U.solved)
                        node.step++;
            if (node.R.id != -1 && node.R.sub > 0)
                if (node.R.point.id != node.id)
                    if (!node.R.solved)
                        node.step++;
            if (node.D.id != -1 && node.D.sub > 0)
                if (node.D.point.id != node.id)
                    if (!node.D.solved)
                        node.step++;

            // target is the root if all adjs CANNOT point to it
            if (node.step == 0) {
                //Debug.Log(Con(node.id) + " is the ROOT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                freedom = true; root = node;
            }
        }
        if (!node.solved) count = validBits.Count;
        else count = 1;
        validBits.Clear(); adj.Clear();
    }
    void NeighbourCheck(Node node, ref Node root, ref int count) {
        if (node.point.id == -1 && node.id != root.id) {
            if (!node.L.solved) adj.Add(node.L);
            if (!node.U.solved) adj.Add(node.U);
            if (!node.R.solved) adj.Add(node.R);
            if (!node.D.solved) adj.Add(node.D);
            if (adj.Count == 1) {
                node.point = adj[0]; //Debug.Log(Con(node.id) + " poitns to " + Con(adj[0].id) + " (aggresively)");
            }
        } if (node.point.id != -1 || node.id == root.id) count = 1;
        else count = adj.Count;
        adj.Clear();
    }
    bool LoopQuestionMark(Node node) {
        for (Node n = node; n.id != -1; n = n.point) {
            if (!validBits.Contains(n.id))
                validBits.Add(n.id);
            else {
                validBits.Clear(); return true; }
        }
        validBits.Clear(); return false;
    }

    bool solvable = true;
    void Solver() {
        List<Node> newList = new List<Node>();
        List<Node> adj = new List<Node>();
        foreach (var node in nodeList) {
            newList.Add(new Node(node.id) {
                U = node.U,
                D = node.D,
                L = node.L,
                R = node.R,
                point = badNode,
                sub = node.sub,
                cbit = node.cbit,
                solved = false
            });
        }
        foreach (var node in newList) {
            if (node.L.id > -1) node.L = newList[node.L.id];
            if (node.U.id > -1) node.U = newList[node.U.id];
            if (node.R.id > -1) node.R = newList[node.R.id];
            if (node.D.id > -1) node.D = newList[node.D.id];
        }
        foreach (var node in newList) {
            if (node.sub == 0) {
                node.solved = true;
                if (node.U.sub > 0) adj.Add(node.U);
                if (node.L.sub > 0) adj.Add(node.L);
                if (node.R.sub > 0) adj.Add(node.R);
                if (node.D.sub > 0) adj.Add(node.D);
                if (adj.Count == 1) {
                    //Debug.Log(Con(node.id) + " poitns to " + Con(adj[0].id));
                    node.point = adj[0];
                }
                adj.Clear();
            }
        }

        int iter = 0; int imafruit = 0;
        bool freedom = false; Node newRoot = badNode;
        while (true) {
            // if the solver can't solve, enters a new "guessing" mdode
            if (iter == 10) {
                var cloneList = PleaseCloneList(newList);
                var Cands = cloneList.FindAll(x => x.point.id == -1);
                if (freedom) Cands.RemoveAll(x => x.id == rootNode.id);
                FindAllCandidates(Cands);

                int iter2 = 0; bool freedom2 = freedom; Node newRoot2 = newRoot; bool failarmy = false;
                List<List<int>> validCandidates = new List<List<int>>();
                int count = 0; int solveCount1 = 0;
                //int iter3 = 0;
                while (true) { 
                    cloneList[FilterCands[0].id].point = FilterCands[0].point;
                    //Debug.Log(Con(FilterCands[0].id) + " > " + Con(FilterCands[0].point.id));
                    while (true) {
                        solveCount1 = cloneList.FindAll(x => x.sub > 0 && x.solved == false).Count;
                        //Debug.Log(iter2.ToString() + "----------------------------------------------------------");
                        //Debug.Log("UGH count: " + solveCount1);
                        if (iter2 == 10) { //Debug.Log("dead-end???");
                            break; } 
                        if (solveCount1 == 0 && freedom2) { //Debug.Log("it solved!!!");
                            break; }

                        // do normal color check + loop and if a node has NO SOLUTIONS
                        foreach (var node in cloneList){
                            ColorCheck(node, ref newRoot2, ref freedom2, ref count);
                            if (count == 0) { failarmy = true; //Debug.Log(Con(node.id) +  " CANT be POINTED TO by a thing!!!");
                                break; }
                            if (LoopQuestionMark(node)) { failarmy = true; //Debug.Log("IT LOOPS!!!");
                                break; }
                        } if (failarmy) break;

                        // do normal neighbour check + loop and if a node has NO SOLUTIONS
                        foreach (var node in cloneList){
                            if (freedom2 || node.sub == 0) {
                                NeighbourCheck(node, ref newRoot2, ref count);
                                if (count == 0) { failarmy = true; //Debug.Log(Con(node.id) + " CANT POINT TO NOTHIN!!!");
                                    break; }
                                if (LoopQuestionMark(node)) { failarmy = true; //Debug.Log("IT LOOPS!!!");
                                    break; }
                            }
                        } if (failarmy) break;

                        if (solveCount1 == cloneList.FindAll(x => x.sub > 0 && x.solved == false).Count)
                            iter2++;

                        RegenRgb();
                        for (int i = 0; i < nodeList.Count; i++)
                            cloneList[i].cbit = nodeList[i].cbit;
                    }
                    iter2 = 0; 
                    if (!failarmy) {
                        var psblty = new List<int>();
                        foreach (var cand in Cands) {
                            psblty.Add(cloneList[cand.id].point.id);
                            if (cloneList[cand.id].point.id > -1)
                                cand.step++;
                        } validCandidates.Add(psblty);
                        for (int i = 0; i < Cands.Count; i++)
                            FilterCands.RemoveAll(x => x.id == Cands[i].id && x.point.id == psblty[i]);
                    }
                    else
                        FilterCands.RemoveAt(0);

                    ResetList(newList, cloneList);
                    freedom2 = freedom; newRoot2 = newRoot; failarmy = false;
                    if (FilterCands.Count == 0) { //Debug.Log("candidates ran dry!!!");
                        break; }
                    //iter3++; Debug.Log("tryina move to NEXT caiddiate " + iter3.ToString());
                }

                // debuggin candidate solutions. for meself
                /*string guh = "";
                foreach (var psblty in validCandidates) {
                    guh = "";
                    for (int i = 0; i < Cands.Count; i++) {
                        if (psblty[i] == -1) guh += "-_-,";
                        else if (Cands[i].L.id == psblty[i]) guh += Con(Cands[i].id).ToLower() + "l,";
                        else if (Cands[i].U.id == psblty[i]) guh += Con(Cands[i].id).ToLower() + "u,";
                        else if (Cands[i].R.id == psblty[i]) guh += Con(Cands[i].id).ToLower() + "r,";
                        else if (Cands[i].D.id == psblty[i]) guh += Con(Cands[i].id).ToLower() + "d,";
                    } Debug.Log(guh);
                }*/
                
                bool nuhuh = false;
                int valCount = 1 << validCandidates.Count;
                for (int val = 1; val < valCount; val++) {
                    var curList = Enumerable.Repeat(-1, Cands.Count).ToList();
                    for (int i = 0; i < validCandidates.Count; i++)
                        if ((val & (1 << i)) != 0) {
                            for (int i2 = 0; i2 < Cands.Count; i2++) {
                                int id = validCandidates[i][i2];
                                if (id != -1) {
                                    if (curList[i2] == -1)
                                        curList[i2] = id;
                                    // if puzzle pieces don match
                                    else if (curList[i2] != id) {
                                        nuhuh = true; //Debug.Log(val.ToString() + "---" + Convert.ToString(val, 2).PadLeft(validCandidates.Count, '0') + ": PUZZLE PIECES DON MATCH!!!");
                                        break;
                                    }
                                }
                            }
                            if (nuhuh) break;
                        }
                    // if candidate doesn't solve enough nodes
                    if (!nuhuh) {
                        int blblbl = curList.Count(x => x == -1);
                        if (freedom) { if (blblbl > 0) nuhuh = true; }
                        else { if (blblbl > 1) nuhuh = true; }
                        //if (nuhuh) Debug.Log(val.ToString() + "---" + Convert.ToString(val, 2).PadLeft(validCandidates.Count, '0') + ": TOO MANY UNSOLVED!!!");
                    }
                    // if candidate already exists
                    if (!nuhuh) 
                        foreach (var item in valCombos) 
                            if (item.SequenceEqual(curList)) {
                                nuhuh = true; //Debug.Log(val.ToString() + "---" + Convert.ToString(val, 2).PadLeft(validCandidates.Count, '0') + ": ANSWER ALREADY EXISTS!!!");
                                break;
                            }
                    if (!nuhuh) {
                        for (int i = 0; i < Cands.Count; i++) 
                            if (curList[i] != -1)
                                cloneList[Cands[i].id].point = cloneList[curList[i]];
                       
                        int pointless = 0; int pointedless = 0;
                        foreach (var node in cloneList) {
                            // if candidate creates a faux leaf
                            if (node.sub > 0) {
                                if (node.L.point.id == node.id) pointedless++;
                                if (node.U.point.id == node.id) pointedless++;
                                if (node.R.point.id == node.id) pointedless++;
                                if (node.D.point.id == node.id) pointedless++;
                                if (pointedless == 0) { nuhuh = true;
                                    //Debug.Log(val.ToString() + "---" + Convert.ToString(val, 2).PadLeft(validCandidates.Count, '0') + ": " + Con(node.id) + " AINT A DAMN LEAF!!!");
                                    break; }
                            }
                            if (nuhuh) break;
                            // final color check
                            ColorCheck(node, ref newRoot2, ref freedom2, ref count);
                            if (count == 0) { nuhuh = true; //Debug.Log(Convert.ToString(val, 2).PadLeft(validCandidates.Count, '0') + ": " + Con(node.id) + " CANT be POINTED TO by a thing!!!");
                                break; }
                            if (nuhuh) break;
                            // final loop check
                            if (LoopQuestionMark(node)) { nuhuh = true; //Debug.Log(Convert.ToString(val, 2).PadLeft(validCandidates.Count, '0') + ": IT LOOPS!!!");
                                break; }
                            if (nuhuh) break;
                            if (node.point.id == -1) pointless++;
                        }
                        // if candidate creates a rootless or rootfull tree 
                        if (pointless != 1) {
                            nuhuh = true; //Debug.Log(val.ToString() + "---" + Convert.ToString(val, 2).PadLeft(validCandidates.Count, '0') + ": TOO MANY POINTERLESSINS!!!!!!!!");
                        }
                    }
                    if (!nuhuh)
                        valCombos.Add(curList);
                    ResetList(newList, cloneList);
                    nuhuh = false;
                }
                
                /*Debug.Log("-------------------------------------------------------------------");
                foreach (var psblty in valCombos) {
                    guh = "";
                    for (int i = 0; i < Cands.Count; i++) {
                        if (psblty[i] == -1) guh += "-_-,";
                        else if (Cands[i].L.id == psblty[i]) guh += Con(Cands[i].id).ToLower() + "l,";
                        else if (Cands[i].U.id == psblty[i]) guh += Con(Cands[i].id).ToLower() + "u,";
                        else if (Cands[i].R.id == psblty[i]) guh += Con(Cands[i].id).ToLower() + "r,";
                        else if (Cands[i].D.id == psblty[i]) guh += Con(Cands[i].id).ToLower() + "d,";
                    } Debug.Log(guh);
                }*/

                if (valCombos.Count > 1) {
                    for (int i = 0; i < Cands.Count; i++) {
                        //Debug.Log(i.ToString() + "---" + Cands.Count().ToString());
                        if (i >= Cands.Count) break;
                        nuhuh = false;
                        foreach (var item in valCombos) {
                            if (item[i] != valCombos[0][i]) { 
                                nuhuh = true; break;
                            }
                        }
                        if (!nuhuh && valCombos[0][i] != -1) { 
                            newList[Cands[i].id].point = newList[valCombos[0][i]];
                            Cands.RemoveAt(i);
                            foreach (var item in valCombos) { 
                                item.RemoveAt(i);
                            } i--;
                        }
                    }

                    foreach (var item in Cands)
                        GLOB += Con(item.id) + " ";
                    
                    foreach (var node in nodeList)
                        node.solved = node.sub == 0 ? false : true;

                    ambigousList.AddRange(newList.FindAll(x => x.point.id == -1));
                    if (freedom) ambigousList.RemoveAll(x => x.id == rootNode.id);
                    foreach (var node in ambigousList)
                        nodeList[node.id].solved = false;

                    solvable = false;
                }

                /*Debug.Log("-------------------------------------------------------------------");
                foreach (var psblty in valCombos) {
                    guh = "";
                    for (int i = 0; i < Cands.Count; i++) {
                        if (psblty[i] == -1) guh += "-_-,";
                        else if (Cands[i].L.id == psblty[i]) guh += Con(Cands[i].id).ToLower() + "l,";
                        else if (Cands[i].U.id == psblty[i]) guh += Con(Cands[i].id).ToLower() + "u,";
                        else if (Cands[i].R.id == psblty[i]) guh += Con(Cands[i].id).ToLower() + "r,";
                        else if (Cands[i].D.id == psblty[i]) guh += Con(Cands[i].id).ToLower() + "d,";
                    } Debug.Log(guh);
                }*/

                break;
            }


            // MAIN SOLVER
            //Debug.Log(iter.ToString() + "----------------------------------------------------------");
            int solveCount = newList.FindAll(x => x.sub > 0 && x.solved == false).Count; //Debug.Log("UGH count: " + solveCount);
            if (solveCount == 0 && freedom) break;
            // points neighbours to target based on how its target must be solved
            foreach (var node in newList)
                ColorCheck(node, ref newRoot, ref freedom, ref imafruit);
            // if root was found, check if there's only one neighbour the target can point to
            foreach (var node in newList) 
                if (freedom || node.sub == 0)
                    NeighbourCheck(node, ref newRoot, ref imafruit);

            RegenRgb();
            for (int i = 0; i < nodeList.Count; i++)
                newList[i].cbit = nodeList[i].cbit;

            if (solveCount == newList.FindAll(x => x.sub > 0 && x.solved == false).Count)
                iter++;
        }
    }
    string GLOB = "";

    bool submission = false, failure = false, transgender = false, themode = false;
    IEnumerator Solvin() {
        yield return new WaitForSeconds(.2f);
        foreach (var node in nodeList) { 
            StartCoroutine(OtherColorChange(node.bt, Color.black));
            if (colorblindModeEnabled)
                node.bt.tm.text = OtherDeterminer(node.cbit);
        }
        aud.Play();
        var adj = new List<Node> { rootNode };
        var adj2 = new List<Node>();
        for (int step = 0; step < validTargets[0].step + 1; step++) {
            adj2.Clear();
            foreach (var node in adj) {
                if (node.step == step) {
                    StartCoroutine(OtherColorChange(node.bt, new Color(0f, .6f, 0f)));
                    if (node.L.point == node) adj2.Add(node.L);
                    if (node.U.point == node) adj2.Add(node.U);
                    if (node.R.point == node) adj2.Add(node.R);
                    if (node.D.point == node) adj2.Add(node.D);
                }
                else if (node.step < step){
                    if (node.sub == 0) continue;
                    else StartCoroutine(OtherColorChange(node.bt,
                        new Color((float)(node.step + 1) / (step + 1) * .6f,
                                  (float)(node.step + 1) / (step + 1) * .25f, 0f)));
                }
            }
            adj.AddRange(adj2);
            aud.volume = .05f * ((validTargets[0].step - step) / (float)validTargets[0].step);
            yield return new WaitForSeconds(.2f);
        } aud.Stop();
        foreach (var node in validTargets)
            StartCoroutine(OtherColorChange(node.bt, new Color(.6f, 0f, .6f)));

    }
    IEnumerator Failure(Button bt) {
        if (colorblindModeEnabled)
            foreach (var node in nodeList) {
                validBits.Add(node.cbit);
                if (node.id != bt.id)
                    node.cbit = 0;
            }
        yield return new WaitForSeconds(.2f);
        Audio.PlaySoundAtTransform("strike_click", transform);
        foreach (var node in nodeList) 
            if (node.id != bt.id) 
                StartCoroutine(ColorChange(node.bt, node.bt.col, Color.red));
        yield return new WaitForSeconds(2f);
        foreach (var node in nodeList)
            if (node.id != bt.id) {
                if (colorblindModeEnabled)
                    node.cbit = validBits[node.id];
                StartCoroutine(ColorChange(node.bt, Color.red, Determiner(node))); }
        yield return new WaitForSeconds(.5f);
        if (colorblindModeEnabled)
            validBits.Clear();
        failure = false;
    }

    void QuickColorChange(Button bt, Color col) {
        bt.mat.SetColor("_Color", col);
        bt.light.color = col; bt.col = col;
    }
    IEnumerator ColorChange(Button bt, Color prev, Color next) {
        float t = 0f;
        while (t < 1) {
            bt.mat.SetColor("_Color", Color.Lerp(prev, Color.black, t));
            bt.light.color = Color.Lerp(prev, Color.black, t);
            if (colorblindModeEnabled)
                bt.tm.color = new Color(bt.tm.color.r, bt.tm.color.g, bt.tm.color.b, 1 - t);
            t += .083f;
            yield return new WaitForSeconds(0.001f);
        }
        bt.mat.SetColor("_Color", Color.black);
        bt.light.color = Color.black;
        if (colorblindModeEnabled)
            bt.tm.text = OtherDeterminer(bt.node.cbit);
        t = 0f;
        while (t < 1) {
            bt.mat.SetColor("_Color", Color.Lerp(Color.black, next, t));
            bt.light.color = Color.Lerp(Color.black, next, t);
            if (colorblindModeEnabled)
                bt.tm.color = new Color(bt.tm.color.r, bt.tm.color.g, bt.tm.color.b, t);
            t += .083f;
            yield return new WaitForSeconds(0.001f);
        }
        bt.mat.SetColor("_Color", next);
        bt.light.color = next; bt.col = next;
        transgender = false; bt.trans = false;
    }
    IEnumerator OtherColorChange(Button bt, Color next)
    {
        float t = 0f;
        if (colorblindModeEnabled)
            bt.tm.text = "";
        while (t < 1) {
            bt.mat.SetColor("_Color", Color.Lerp(bt.col, next, t));
            bt.light.color = Color.Lerp(bt.col, next, t);
            if (colorblindModeEnabled)
                bt.tm.color = new Color(bt.tm.color.r, bt.tm.color.g, bt.tm.color.b, 1 - t);
            t += .083f;
            yield return new WaitForSeconds(0.001f);
        }
        bt.mat.SetColor("_Color", next);
        bt.light.color = next;
        bt.col = next;
        if (colorblindModeEnabled)
            bt.tm.text = OtherDeterminer(bt.node.cbit);
    }
    IEnumerator Push(Button bt)
    {
        float t = 0f; var pos = bt.obj.transform.localPosition;
        while (t < 1)  {
            var obj = bt.obj;
            var position = obj.transform.localPosition; position.y -= .001f;
            obj.transform.localPosition = position;
            t += .1f;
            yield return new WaitForSeconds(0.001f);
        }
        t = 0f;
        while (t < 1) {
            var obj = bt.obj;
            var position = obj.transform.localPosition; position.y += .001f;
            obj.transform.localPosition = position;
            t += .1f;
            yield return new WaitForSeconds(0.001f);
        }
        bt.obj.transform.localPosition = pos;
        bt.trans = false;
    }

    void RegenRgb() {
        foreach (var node in nodeList)
            node.cbit = 0;

        Node n;
        foreach (var leaf in leafNodes) {
            leaf.cbit = 1 << Ran(0, 2);
            var cbit = leaf.cbit;
            n = leaf.point;
            n.cbit ^= cbit;
            while (n.point.id != -1) {
                n.point.cbit ^= cbit;
                n = n.point;
            }
        }
    }
    Color Determiner(Node node)  {
        var cbit = node.cbit;
        float num = node.sub == 0 ? .25f : .6f;
        return new Color(
            (cbit & 1) != 0 ? num : 0f,
            (cbit & 2) != 0 ? num : 0f,
            (cbit & 4) != 0 ? num : 0f);
    }
    string OtherDeterminer(int cbit) {
        switch (cbit) {
            case 1: return "R";
            case 2: return "G";
            case 3: return "Y";
            case 4: return "B";
            case 5: return "M";
            case 6: return "C";
        } return "";
    }

    // self explanatory
    IEnumerator StupidAssDumbAssPieceOfFuckYou() {
        yield return new WaitForSeconds(.25f);
        float scalar = transform.lossyScale.x;

        for (int i = 0; i < nodeList.Count; i++)
            nodeList[i].bt.light.range *= scalar;

    }
    string uggghghhGHHAAAAAAAAA = "654321ABCDEF";
    string Con(int id) { return uggghghhGHHAAAAAAAAA[(id % 6) + 6] + uggghghhGHHAAAAAAAAA[CY(id)].ToString(); }
    void Start () {
        StartCoroutine(StupidAssDumbAssPieceOfFuckYou());
        colorblindModeEnabled = ColorblindMode.ColorblindModeActive;
        if (colorblindModeEnabled) {
            foreach (var bt in buttonList) {
                bt.tm = bt.obj.GetComponentInChildren<TextMesh>();
                bt.tm.text = OtherDeterminer(bt.node.cbit);
                if (bt.node.sub == 0) bt.tm.color = Color.white;
            }
        }
    }


    public class Button
    {
        public GameObject obj;
        public Light light;
        public Material mat;
        public Node node;
        public KMSelectable sel;
        public TextMesh tm;
        public Color col;
        public int colorID;
        public int id;
        public bool trans = false;

        public Button(GameObject O, int ID, Node N)
        {
            obj = O; id = ID; node = N;
            light = obj.GetComponentInChildren<Light>();
            mat = obj.GetComponent<MeshRenderer>().material;
            sel = obj.GetComponentInChildren<KMSelectable>();
            col = Color.black;
        }
    }
    public class Node
    {
        public string col;
        public int id;
        public int cbit = 0;
        public int sub = 0;
        public int step = 0;
        public bool solved = false;

        public Button bt;
        public Node point;
        public Node U;
        public Node D;
        public Node L;
        public Node R;

        public Node(int ID)
        {
            id = ID;
        }

    }
    int CY(int id) { return (id >= 0) ? (int)Mathf.Floor(id / 6) : (int)Mathf.Floor(id / 6) - 1; }
    int Ran(int a, int b) { return Random.Range(a, b + 1); }


#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"'!{0} a1' to press a1, (leftmost column = a, topmost row = 1). Button presses can be chained. e.g. '!{0} a2 a3 b3 c3'";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToLower();
        string[] input = command.Split(' ');

        if (command.Any(x => !"654321abcdef ".Contains(x))) {
            char c = command.First(x => !"654321abcdef ".Contains(x));
            yield return "sendtochaterror The character (" + c + ") is not valid!";
        }
        else
        {
            bool valid = true;
            foreach (var item in input) {
                if (item.Length != 2) {
                    yield return "sendtochaterror Button coordinate '" + item + "' is not 2 characters long!";
                    valid = false; break;
                }
                if (!"abcdef".Contains(item[0]) || !"654321".Contains(item[1])) {
                    yield return "sendtochaterror Button coordinate '" + item + "' is not formatted as LETTER + DIGIT!";
                    valid = false; break;
                }
            }
            if (valid) {
                foreach (var item in input) {
                    int id = "abcdef".IndexOf(item[0]) + ("654321".IndexOf(item[1]) * 6);
                    nodeList[id].bt.sel.OnInteract();
                    if (failure) {
                        yield return "sendtochaterror Button '" + item + "' resulted in a strike!";
                        break;
                    }
                    else {
                        if (submission)
                            yield return new WaitForSeconds(0.2f);
                        else
                            yield return new WaitForSeconds(0.5f);
                    }
                }
            }
        }
    }

}
