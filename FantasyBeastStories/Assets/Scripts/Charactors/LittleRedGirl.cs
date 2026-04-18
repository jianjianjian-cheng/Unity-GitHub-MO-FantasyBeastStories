using System.Collections;
using System.Collections.Generic;
using Charactors;
using Charactors.Pets;
using Photon.Pun;
using UnityEngine;

public class LittleRedGirl : PlayerController
{
    [SerializeField] private GameObject charmanderPet; // charmander宠物预制体
    List<GameObject> petList = new List<GameObject>(); // 宠物列表
    protected override void Awake()
    {
        base.Awake();
        // 实例化宠物
        GameObject pet = PhotonNetwork.Instantiate(charmanderPet.name, transform.position, Quaternion.identity);
        pet.transform.SetParent(transform.parent);
        pet.GetComponent<Charmander>().SetHostPlayer(this.gameObject); // 设置宠物的主人玩家
        petList.Add(pet);
    }

    //实例化并添加宠物的方法
    public void AddPet(GameObject petPrefab)
    {
        GameObject pet = PhotonNetwork.Instantiate(petPrefab.name, transform.position, Quaternion.identity);
        petList.Add(pet);
    }
}
