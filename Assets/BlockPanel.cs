using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public enum VECTOR
{
    UP,
    DOWN,
    LEFT, 
    RIGHT
}

public class BlockPanel : MonoBehaviour
{
    public static BlockPanel Instance { get; private set; }

    const int BLOCK_WIDTH = 6;
    const int BLOCK_HEIGHT = 6;
    
    [SerializeField] Block blockPrefab;

    public bool IsLockBlock;
    Block[] blocks;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GenerateBlock();
    }

    [ContextMenu("새로운 블록 만들기")]
    public void GenerateBlock()
    {
        //블록 배열이 없으면 개수에 맞춰 생성한다
        if (blocks == null)
            blocks = new Block[BLOCK_WIDTH * BLOCK_HEIGHT];

        //블록들을 생성하기 전 기존 블록들을 삭제
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] != null)
                DestroyImmediate(blocks[i].gameObject);
        }

        //배열에 저장되지 않은 블록을 찾아 에디터 상에서 삭제
        Block[] anotherBlocks = FindObjectsOfType<Block>();
        foreach (Block block in anotherBlocks)
            DestroyImmediate(block.gameObject);

        //블록 생성
        Vector3 pivot = new Vector3((BLOCK_WIDTH / 2 - 0.5f) * -1f, BLOCK_HEIGHT / 2 - 0.5f, 0);
        for (int i = 0; i < blocks.Length; i++)
        {
            //블록의 index와 x,y값을 계산한 뒤 위치를 잡는다
            Block block = Instantiate(blockPrefab, transform);
            block.Setup(i, i % BLOCK_WIDTH, i / BLOCK_HEIGHT);
            block.transform.localPosition = pivot + new Vector3(block.x, -block.y);
            block.transform.localScale = Vector3.one;
            blocks[i] = block;
        }

        //중복 블록 체크
        foreach(Block block in blocks)
        {
            for(int i=0;i<2;i++)
            {
                while(true)
                {
                    Block[] connected = ConnectedBlock(block, i == 0);
                    if (connected.Length >= 3)
                        block.Change();
                    else
                        break;
                }
            }
        }
    }

    private Block[]ConnectedBlock(Block block,bool isHorizontal)
    {
        //최소, 최대 인덱스와 더해가는 값
        int minIndex = isHorizontal ? block.y * BLOCK_WIDTH : block.x;
        int maxIndex = isHorizontal ? minIndex + BLOCK_WIDTH - 1 : minIndex + (BLOCK_WIDTH * (BLOCK_HEIGHT - 1));
        int addValue = isHorizontal ? 1 : BLOCK_WIDTH;

        List<Block> result = new List<Block>();
        for(int index=minIndex; index<=maxIndex; index+=addValue)
        {
            List<Block> check = new List<Block>();
            for(int i=index;i<=maxIndex;i+=addValue)
            {
                if (block.id == blocks[i].id)
                    check.Add(blocks[i]);
                else
                    break;
            }

            if (check.Contains(block) && check.Count > result.Count)
                result = check;
        }
        return result.ToArray();
    }

    public bool SwapBlock(int index,VECTOR dir)
    {
        int targetIndex = index;
        targetIndex += dir switch
        {
            VECTOR.UP => -BLOCK_WIDTH,
            VECTOR.DOWN => -BLOCK_WIDTH,
            VECTOR.LEFT => -1,
            VECTOR.RIGHT => -1,
            _ => 0
        };

        if (targetIndex < 0 || targetIndex >= blocks.Length)
            return false;

        StartCoroutine(IECheckSwap(index, targetIndex));
        return true;
    }

    IEnumerator IECheckSwap(int index,int targetIndex)
    {
        IsLockBlock = true;
        Block block = blocks[index];
        Block targetBlock = blocks[targetIndex];
        yield return StartCoroutine(IESwap(block, targetBlock));    //스왑이 끝날때까지 대기

        //블록 체크 (3개이상이면 터트리기)

        yield return StartCoroutine(IESwap(block, targetBlock));
        IsLockBlock = false;
    }

    IEnumerator IESwap(Block blockA,Block blockB)
    {
        const float SWAP_SPEED = 5f;

        //교환
        Vector3 posA = blockB.transform.localPosition;
        Vector3 posB = blockA.transform.localPosition;
        while(blockA.transform.localPosition!= posA||blockB.transform.localPosition != posB) 
        {
            blockA.transform.localPosition = Vector3.MoveTowards(blockA.transform.localPosition, posA, SWAP_SPEED * Time.deltaTime);
            blockB.transform.localPosition = Vector3.MoveTowards(blockB.transform.localPosition, posB, SWAP_SPEED * Time.deltaTime);
            yield return null;
        }

        blockA.SwapBlock(blockB);
    }

    private Block[]GroupX(int index)
    {
        List<Block> result = new List<Block>();
        Block block = blocks[index];
        int min = (int)(index * BLOCK_WIDTH) * BLOCK_WIDTH;
        int max = min + BLOCK_WIDTH - 1;

        int p = index - 1;
        while(index>=min)
        {
            if (blocks[p].id == block.id)
                result.Add(blocks[p--]);
            else
                break;
        }
        p = index + 1;
        while (index <= max)
        {
            if (blocks[p].id == block.id)
                result.Add(blocks[p++]);
            else
                break;
        }

        return result.ToArray();
    }
}
