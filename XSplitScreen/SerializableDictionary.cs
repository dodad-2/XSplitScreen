
using System.Collections.Generic;
using System;
using UnityEngine;
using JetBrains.Annotations;

/// <summary>
/// A serializable multidimensional string dictionary
/// </summary>
[Serializable]
public class MultiDictionary : SerializableDictionary<string, StringDictionary> { }

/// <summary>
/// A serializable string dictionary
/// </summary>
[Serializable]
public class StringDictionary : SerializableDictionary<string, string> { }

/// <summary>
/// A serializable int dictionary
/// </summary>
[Serializable]
public class IntDictionary : SerializableDictionary<int, string> { }

[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
	[SerializeField]
	private List<TKey> keys = new List<TKey>();

	[SerializeField]
	private List<TValue> values = new List<TValue>();

	public void OnBeforeSerialize()
	{
		keys.Clear();
		values.Clear();

		foreach (KeyValuePair<TKey, TValue> pair in this)
		{
			keys.Add(pair.Key);
			values.Add(pair.Value);
		}
	}

	public void OnAfterDeserialize()
	{
		this.Clear();

		if (keys.Count != values.Count)
			throw new System.Exception(string.Format("There are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."));

		for (int i = 0; i < keys.Count; i++)
			this.Add(keys[i], values[i]);
	}
}