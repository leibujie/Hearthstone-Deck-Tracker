﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;

#endregion

namespace Hearthstone_Deck_Tracker
{
	public class OpponentSecrets
	{
		public OpponentSecrets()
		{
			Secrets = new List<SecretHelper>();
		}

		public List<SecretHelper> Secrets { get; private set; }

		public List<HeroClass> DisplayedClasses
		{
			get { return Secrets.Select(x => x.HeroClass).Distinct().OrderBy(x => x).ToList(); }
		}

		public int GetIndexOffset(HeroClass heroClass)
		{
			switch(heroClass)
			{
				case HeroClass.Hunter:
					return 0;
				case HeroClass.Mage:
					if(DisplayedClasses.Contains(HeroClass.Hunter))
						return SecretHelper.GetMaxSecretCount(HeroClass.Hunter);
					return 0;
				case HeroClass.Paladin:
					if(DisplayedClasses.Contains(HeroClass.Hunter) && DisplayedClasses.Contains(HeroClass.Mage))
						return SecretHelper.GetMaxSecretCount(HeroClass.Hunter) + SecretHelper.GetMaxSecretCount(HeroClass.Mage);
					if(DisplayedClasses.Contains(HeroClass.Hunter))
						return SecretHelper.GetMaxSecretCount(HeroClass.Hunter);
					if(DisplayedClasses.Contains(HeroClass.Mage))
						return SecretHelper.GetMaxSecretCount(HeroClass.Mage);
					return 0;
			}
			return 0;
		}

		public HeroClass? GetHeroClass(string cardId)
		{
			HeroClass heroClass;
			if(!Enum.TryParse(Database.GetCardFromId(cardId).PlayerClass, out heroClass))
				return null;
			return heroClass;
		}

		public void NewSecretPlayed(HeroClass heroClass, int id, bool stolen)
		{
			Secrets.Add(new SecretHelper(heroClass, id, stolen));
			Logger.WriteLine("Added secret with id:" + id, "OpponentSecrets");
		}

		public void SecretRemoved(int id)
		{
			var secret = Secrets.FirstOrDefault(s => s.Id == id);
			Secrets.Remove(secret);
			Logger.WriteLine("Removed secret with id:" + id, "OpponentSecrets");
		}

		public void ClearSecrets()
		{
			Secrets.Clear();
			Logger.WriteLine("Cleared secrets", "OpponentSecrets");
		}

		public void LogSecretState()
		{
			Logger.WriteLine("Secrets. Count: " + Secrets.Count, "OpponentSecrets");
			int i = 0;
			foreach (var secret in Secrets)
			{
				Logger.WriteLine("Secret " + i + " Stolen: " + secret.Stolen + " HeroClass " + secret.HeroClass.ToString());
				foreach(var poss in secret.PossibleSecrets)
				{
					Logger.WriteLine(poss.Key + " " + poss.Value);
				}
				i++;
			}
		}

		// TODO: maybe deprecate this? if we track game state, don't need to click to toggle
		public void Trigger(string cardId)
		{
			var heroClass = GetHeroClass(cardId);
			if(!heroClass.HasValue)
				return;
			if(Secrets.Where(s => s.HeroClass == heroClass).Any(s => s.PossibleSecrets[cardId]))
				SetZero(cardId, heroClass.Value);
			else
				SetMax(cardId, heroClass.Value);
		}

		public void SetMax(string cardId, HeroClass? heroClass)
		{
			if(heroClass == null)
			{
				heroClass = GetHeroClass(cardId);
				if(!heroClass.HasValue)
					return;
			}

			foreach(var secret in Secrets.Where(s => s.HeroClass == heroClass))
			{
				secret.PossibleSecrets[cardId] = true;
			}

			LogSecretState();
		}

		public void SetZero(string cardId, HeroClass? heroClass)
		{
			if(heroClass == null)
			{
				heroClass = GetHeroClass(cardId);
				if(!heroClass.HasValue)
					return;
			}

			foreach (var secret in Secrets.Where(s => s.HeroClass == heroClass))
			{
				secret.PossibleSecrets[cardId] = false;
			}

			LogSecretState();
		}

		public List<Secret> GetSecrets()
		{
			LogSecretState();
			var returnThis = DisplayedClasses.SelectMany(SecretHelper.GetSecretIds).Select(cardId => new Secret(cardId, 0)).ToList();

			foreach (var secret in Secrets)
			{
				foreach (var possible in secret.PossibleSecrets)
				{
					if (possible.Value)
					{
						returnThis.Find(x => x.CardId == possible.Key).Count = 1;
					}
				}

			}

			return returnThis;
		}

		public List<Secret> GetDefaultSecrets(HeroClass heroClass)
		{
			var count = SecretHelper.GetMaxSecretCount(heroClass);
			var returnThis = new List<Secret>();

			foreach(var cardId in SecretHelper.GetSecretIds(heroClass))
				returnThis.Add(new Secret(cardId, 1));

			return returnThis;
		}
	}
}