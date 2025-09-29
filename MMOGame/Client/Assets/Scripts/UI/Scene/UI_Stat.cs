using Data;
using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.UI;

public class UI_Stat : UI_Base
{
    enum Images
    {
        Slot_Helmet,
        Slot_Armor,
        Slot_Boots,
        Slot_Weapon,
        Slot_Shield
    }

    enum Texts
    {
        NameText,
        AttackValueText,
        DefenceValueText
    }

    bool _init = false;
    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<Text>(typeof(Texts));

        _init = true;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_init == false)
            return;

        // 스탯창 UI의 하위 UI들 중에서 착용한 장비의 UI의 Image 컴포넌트 비활성화
        Get<Image>((int)Images.Slot_Helmet).enabled = false;
        Get<Image>((int)Images.Slot_Armor).enabled = false;
        Get<Image>((int)Images.Slot_Boots).enabled = false;
        Get<Image>((int)Images.Slot_Weapon).enabled = false;
        Get<Image>((int)Images.Slot_Shield).enabled = false;

        // 아이템을 순회하면서 착용한 장비를 UI에 표시
        foreach (Item item in Managers.Inven.Items.Values)
        {
            if (item.Equipped == false)
                continue;

            ItemData itemData = null;
            Managers.Data.ItemDict.TryGetValue(item.TemplateId, out itemData);
            Sprite icon = Managers.Resource.Load<Sprite>(itemData.iconPath);

            if (item.ItemType == ItemType.Weapon)
            {
                Get<Image>((int)Images.Slot_Weapon).enabled = true;
                Get<Image>((int)Images.Slot_Weapon).sprite = icon;
            }
            else if (item.ItemType == ItemType.Armor)
            {
                Armor armor = (Armor)item;
                switch (armor.ArmorType)
                {
                    case ArmorType.Helmet:
                        Get<Image>((int)Images.Slot_Helmet).enabled = true;
                        Get<Image>((int)Images.Slot_Helmet).sprite = icon;
                        break;
                    case ArmorType.Armor:
                        Get<Image>((int)Images.Slot_Armor).enabled = true;
                        Get<Image>((int)Images.Slot_Armor).sprite = icon;
                        break;
                    case ArmorType.Boots:
                        Get<Image>((int)Images.Slot_Boots).enabled = true;
                        Get<Image>((int)Images.Slot_Boots).sprite = icon;
                        break;
                }
            }
        }

        // 플레이어와 연관된 Text UI 표시
        MyPlayerController player = Managers.Object.MyPlayer;
        player.RefreshAdditionalStat();

        Get<Text>((int)Texts.NameText).text = player.name;

        int totalDamage = player.Stat.Attack + player.WeaponDamage;
        Get<Text>((int)Texts.AttackValueText).text = $"{totalDamage}(+{player.WeaponDamage})";
        Get<Text>((int)Texts.DefenceValueText).text = $"{player.ArmorDefence}";
    }
}
