# p3ppc.unhardcodedBgmIds
This mod is intended to be used as a dependency of others that need to call new BGM in P3P. 

With this set as a dependency, you'll be able to call BGM with ids larger than 126 with the flowscript `BGM` function. The id you pass to the function will correspond to the adx file that is used, for example `BGM(300)` will play the sound found in `data/sound/bgm/300.ADX`.
