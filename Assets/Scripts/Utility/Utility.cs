using UnityEngine;
using System.Collections;
using System;

public class Utility : MonoBehaviour {

	public static int RandomInt(int a)
	{
		return (int) UnityEngine.Random.Range(0, a);
	}

	public static int RandomInt(float a){
		return (int) UnityEngine.Random.Range(0, a);
	}
	public static int RandomIntInclusive(float a){
		return (int) UnityEngine.Random.Range(-a, a);
	}

	public static Vector3 RandomVector(float x = 0.0F, float y = 0.0F, float z = 0.0F)
	{
		return new Vector3(UnityEngine.Random.Range(0, x), UnityEngine.Random.Range(0, y), UnityEngine.Random.Range(0,z));
	}

	public static Vector3 RandomVectorInclusive(float x = 0.0F, float y = 0.0F, float z = 0.0F)
	{
		return new Vector3(UnityEngine.Random.Range(-x, x), UnityEngine.Random.Range(-y, y), UnityEngine.Random.Range(-z,z));
	}

	public static int [] IntNormal(int [] x, int [] y)
	{
		int a = y[0] - x[0];
		int b = y[1] - x[1];
		a = a >= 1 ? 1 : (a <= -1 ? -1 : 0);
		b = b >= 1 ? 1 : (b <= -1 ? -1 : 0);
		return new int [] 
		{
			a,
			b
		};
	}

	public static void Flog(params object [] s)
	{
		string final = "";
		for(int i = 0; i < s.Length; i++)
		{
			final += s[i].ToString();
			if(i < s.Length-1) final += " : ";
		}
		Debug.Log(final);
	}
}

[System.Serializable]
public class IntVector
{
	//public static IntVector zero = new IntVector(0,0);
	public int x, y;
	public int this[int v]
	{
		get
		{
			if(v == 0) return x;
			else if(v == 1) return y;
			else return 0;
		}
		
	}
	public IntVector(int a, int b)
	{
		x = a;
		y = b;
	}

	public IntVector(float a, float b)
	{
		x = (int) a;
		y = (int) b;
	}
	public IntVector(IntVector a)
	{
		x = a.x;
		y = a.y;
	}

	public IntVector(int a)
	{
		x = a;
		y = a;
	}

	public static IntVector operator + (IntVector a, IntVector b)
	{
		return new IntVector(b.x+a.x, b.y+a.y);
	}

	public bool Equals(IntVector b) {return x == b.x && y == b.y;}

	public Vector2 ToVector2
	{
		get{
			return new Vector2(x, y);
		}
	}

	public int [] ToInt
	{
		get{
			return new int[] {x,y};
		}
	}

	public void Add(IntVector a)
	{
		x += a.x;
		y += a.y;
	}

	public void Sub(IntVector a)
	{
		x -= a.x;
		y -= a.y;
	}

	public void Mult(float m)
	{
		x = (int)((float)x*m);
		y = (int)((float)y*m);
	}
}

