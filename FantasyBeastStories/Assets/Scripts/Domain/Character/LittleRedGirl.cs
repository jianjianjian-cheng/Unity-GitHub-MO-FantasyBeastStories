using System.Collections;
using System.Collections.Generic;
using Domain.Character;
using Domain.Character.Pets;
using Domain.Event;
using Domain.Services;
using UnityEngine;

namespace Domain.Character
{
    public class LittleRedGirl : PlayerController
    {
        [SerializeField] private GameObject charmanderPet; // charmander宠物预制体
        List<GameObject> petList = new List<GameObject>(); // 宠物列表
        protected override void Awake()
        {
            base.Awake();
            // 实例化宠物
            if (!isOnlyShow || !EventChannelLocator.MainContainer.gameSettings.IsStayLobby)
            {
                GameObject pet = NetworkServiceLocator.ObjectService.Instantiate(charmanderPet.name, transform.position, Quaternion.identity);
                pet.transform.SetParent(transform.parent);
                pet.GetComponent<Charmander>().SetOwner(this.gameObject); // 设置宠物的主人玩家
                petList.Add(pet);
            }
        }

        //实例化并添加宠物的方法
        public void AddPet(GameObject petPrefab)
        {
            GameObject pet = NetworkServiceLocator.ObjectService.Instantiate(petPrefab.name, transform.position, Quaternion.identity);
            petList.Add(pet);
        }
    }
}