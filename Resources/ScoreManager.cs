using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoreManager : MonoBehaviour {

    private int[] TileArray = new int[50];
    private int[] FuritenArray = new int[50];
    private int fu = 0;
    private int han = 0;

    private int sets = 0;
    private int pairs = 0;
    private bool handFound = false;
    private bool furiten = false;
    private int discardedTile;

    private List<int> terminalTiles;
    private List<int> honorTiles;
    private List<int> greenTiles;

    //10 for a run, 1 for a pair, 100 for a set.
    private int[] tileScoringLocations = new int[50];

    public void LoadTileTypes()
    {
        terminalTiles = new List<int>();
        honorTiles = new List<int>();
        greenTiles = new List<int>();

        for (int i = 0; i < 44; i++)
        {
            if ((i % 10 == 8 || i % 10 == 0) && i < 30)
            {
                terminalTiles.Add(i);
            }

            if (i >= 30)
            {
                honorTiles.Add(i);
            }
            if ((i > 10 && i < 20) || i == 41)
            {
                if (i != 14 && i != 16 && i != 18)
                {
                    greenTiles.Add(i);
                }
            }
        }
    }
    public int CalculateScore(int tile)
    {
        discardedTile = tile;
        int score = 0;
        fu += 30;
        han = 1;

        CheckWaits(0, false);
        CalculateFuForWaits();
        int mangan = 8000;
        int haneman = 12000;
        int baiman = 16000;
        int sanbaiman = 24000;
        int kazoeyakuman = 32000;

        if (han < 5)
        {
            double multiplier = System.Math.Pow(2, (2 + han));
            score = fu * (int)multiplier;
        }
        else if (han == 5)
        {
            score = mangan;
        }
        else if (han == 6 || han == 7)
        {
            score = haneman;
        }
        else if (han > 7 && han < 11)
        {
            score = baiman;
        }
        else if (han == 11 || han == 12)
        {
            score = sanbaiman;
        }
        else if (han > 12)
        {
            score = kazoeyakuman;
        }
        Debug.Log("Score: " + score + ", Fu: " + fu + ", Han: " + han);
        return score;
    }
    //TODO: add special scoring for seat winds.
    private int CalculateFuForSet(int pos)
    {
        int tempFu = 0;
        if (terminalTiles.Contains(pos) || honorTiles.Contains(pos))
        {
            tempFu = 8;
        }
        else
        {
            tempFu = 4;
        }
        return tempFu;
    }
    private int CalculateFuForPair(int pos)
    {
        int tempFu = 0;
        if (honorTiles.Contains(pos))
        {
            tempFu = 2;
        }
        return tempFu;
    }
    private void CalculateFuForWaits()
    {
        int waitTile = tileScoringLocations[discardedTile];
        if(waitTile % 100 == 0)
        {
            //wait was set
        }
        else if(waitTile % 10 == 0)
        {
            //wait was run
        }
        else
        {
            //wait was pair
        }

    }
    private void CheckWaits(int pos, bool waitsFound)
    {
        if (pos > 43 || waitsFound)
        {
            return;
        }
        int totalSum = tileScoringLocations.Sum();
        if (totalSum == 41 || totalSum == 131 || totalSum == 221 || totalSum == 311 || totalSum == 401)
        {
            waitsFound = true;
            return;
        }
        if (TileArray[pos] >= 3)
        {
            tileScoringLocations[pos] += 100;
            TileArray[pos] -= 3;
            CheckWaits(pos, waitsFound);
            TileArray[pos] += 3;
            tileScoringLocations[pos] -= 100;
        }
        if ((pos < 27) && (TileArray[pos] >= 1 && TileArray[pos + 1] >= 1 && TileArray[pos + 2] >= 1))
        {

            TileArray[pos] -= 1;
            TileArray[pos + 1] -= 1;
            TileArray[pos + 2] -= 1;
            tileScoringLocations[pos] += 10;
            CheckWaits(pos, waitsFound);
            tileScoringLocations[pos] -= 10;
            TileArray[pos] += 1;
            TileArray[pos + 1] += 1;
            TileArray[pos + 2] += 1;

        }
        if (TileArray[pos] == 2)
        {
            TileArray[pos] -= 2;
            tileScoringLocations[pos] += 1;
            CheckWaits(pos, waitsFound);
            tileScoringLocations[pos] -= 1;
            TileArray[pos] += 2;
        }
        CheckWaits(pos + 1, waitsFound);
    }
    private int CalculateHan(int[] tileArray)
    {
        int han = 0;
        int iterator = 0;
        int pairCount = 0;
        int tripletCount = 0;
        foreach (int i in tileArray)
        {
            if (i == 3)
            {
                tripletCount++;
            }
            if (i == 2)
            {
                pairCount++;
            }
            iterator++;
        }
        //2 han for 7 pairs
        if (pairCount == 7)
        {
            han += 2;
        }
        //2 han for 3 triplets
        if (tripletCount == 3)
        {
            han += 2;
        }
        return han;
    }
    public void CheckForSpecialHands()
    {
        int pairCount = 0;
        int[] thirteenOrphans = new int[13] { 0, 8, 10, 18, 20, 28, 30, 31, 32, 33, 40, 41, 42 };
        int orphanCount = 0;
        for (int i = 0; i < TileArray.Length; i++)
        {
            if (TileArray[i] == 2)
            {
                pairCount++;
            }
            if (TileArray[i] >= 1 && thirteenOrphans.Contains(i))
            {
                orphanCount++;
            }
        }
        if (orphanCount == 13)
        {
            //WinGame((turnNumber + 1) % 2);
            //Win by 13 orphans
            Debug.Log("Won by 13 orphans!");
        }
        if (pairCount == 7)
        {
            //WinGame((turnNumber + 1) % 2);
            //Win by 7 pairs
            Debug.Log("Won by 7 pairs");
        }
    }
    public bool CheckForFuriten(int tilePos, List<Tile> playerHand)
    {
        furiten = false;
        sets = 0;
        pairs = 0;
        int[] tileIntArray = new int[50];
        foreach (Tile t in playerHand)
        {
            tileIntArray[t.GetValue()] += 1;
        }
        tileIntArray[tilePos] += 1;
        FuritenArray = tileIntArray;
        CheckValueForFuriten(0);
        return furiten;
    }
    private void CheckValue(int pos)
    {
        if (pos > 43 || handFound)
        {
            return;
        }
        if ((sets == 4 && pairs == 1))
        {
            handFound = true;
            sets = 0;
            pairs = 0;           
            return;
        }
        if (TileArray[pos] >= 3)
        {
            sets++;
            TileArray[pos] -= 3;
            fu += CalculateFuForSet(pos);
            CheckValue(pos);
            fu -= CalculateFuForSet(pos);
            TileArray[pos] += 3;
            sets--;
        }
        if ((pos < 27) && (TileArray[pos] >= 1 && TileArray[pos + 1] >= 1 && TileArray[pos + 2] >= 1))
        {
            sets++;
            TileArray[pos] -= 1;
            TileArray[pos + 1] -= 1;
            TileArray[pos + 2] -= 1;
            CheckValue(pos);
            TileArray[pos] += 1;
            TileArray[pos + 1] += 1;
            TileArray[pos + 2] += 1;
            sets--;
        }
        if (TileArray[pos] == 2)
        {
            pairs++;
            TileArray[pos] -= 2;
            fu += CalculateFuForPair(pos);
            CheckValue(pos);
            fu += CalculateFuForPair(pos);
            TileArray[pos] += 2;
            pairs--;
        }
        CheckValue(pos + 1);
    }
    private void CheckValueForFuriten(int pos)
    {
        if (pos > 43 || furiten)
        {
            return;
        }
        if ((sets == 4 && pairs == 1))
        {
            furiten = true;
            return;
        }
        if (FuritenArray[pos] >= 3)
        {
            sets++;
            FuritenArray[pos] -= 3;
            CheckValue(pos);
            FuritenArray[pos] += 3;
            sets--;
        }
        if ((pos < 27) && (FuritenArray[pos] >= 1 && FuritenArray[pos + 1] >= 1 && FuritenArray[pos + 2] >= 1))
        {
            sets++;
            FuritenArray[pos] -= 1;
            FuritenArray[pos + 1] -= 1;
            FuritenArray[pos + 2] -= 1;
            CheckValue(pos);
            FuritenArray[pos] += 1;
            FuritenArray[pos + 1] += 1;
            FuritenArray[pos + 2] += 1;
            sets--;
        }
        if (FuritenArray[pos] == 2)
        {
            pairs++;
            FuritenArray[pos] -= 2;
            CheckValue(pos);
            FuritenArray[pos] += 2;
            pairs--;
        }
        CheckValueForFuriten(pos + 1);
    }
    public void SetTileArray(int[] array)
    {
        TileArray = array;
    }
}
