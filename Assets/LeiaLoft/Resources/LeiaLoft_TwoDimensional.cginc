/****************************************************************
*
* Copyright 2019 © Leia Inc.  All rights reserved.
*
* NOTICE:  All information contained herein is, and remains
* the property of Leia Inc. and its suppliers, if any.  The
* intellectual and technical concepts contained herein are
* proprietary to Leia Inc. and its suppliers and may be covered
* by U.S. and Foreign Patents, patents in process, and are
* protected by trade secret or copyright law.  Dissemination of
* this information or reproduction of this materials strictly
* forbidden unless prior written permission is obtained from
* Leia Inc.
*
****************************************************************
*/

#ifndef LEIALOFT_TWO_DIMENSIONAL_INCLUDED
#define LEIALOFT_TWO_DIMENSIONAL_INCLUDED

struct appdata
{
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
};

struct v2f
{
	float2 uv : TEXCOORD0;
	//UNITY_FOG_COORDS(1)
	float4 vertex : SV_POSITION;
};

sampler2D _texture_0;
float4 _texture_0_ST;

	inline v2f ProcessVerts(appdata v)
	{
		v2f o;
	#if UNITY_VERSION >= 560
		o.vertex = UnityObjectToClipPos(v.vertex);
	#else
		o.vertex = UnityObjectToClipPos(v.vertex);
	#endif
		o.uv = TRANSFORM_TEX(v.uv, _texture_0);
		return o;
	}
	
	inline fixed4 ProcessFragment(v2f i)
	{
		//return tex2D(_MainTex, i.uv)/2;
		return tex2D(_texture_0, i.uv);
	}

#endif // LEIALOFT_TWO_DIMENSIONAL_INCLUDED