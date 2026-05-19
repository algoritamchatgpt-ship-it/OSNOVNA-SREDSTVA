PARAMETERS SAKOL
PUSH KEY CLEAR
SELECT OSPOPIS
MMESTO=MESTO
MMTR=MTR
IF MMTR<1
   MMTR=0
ENDIF

MAG=AG
MAGPOD=AGPOD
MGRUPA=GRUPA
mmkonto=konto
MKONTO=ALLTRIM(KONTO)
mlen=LEN(ALLTRIM(konto))
SELECT OS0

SET FILTER TO EMPTY(datprod)
GO top

IF SAKOL=1
*REPORT FORM OSPOPIS0 PREVIEW FOR (MESTO=MMESTO.OR.MMESTO=SPACE(4));
                           .AND.(MTR=MMTR.OR.MMTR=0);
                           .AND.(AG=MAG.OR.MAG=SPACE(1));
                           .AND.(AGPOD=MAGPOD.OR.MAGPOD=SPACE(2));
                           .AND.(SUBSTR(KONTO,1,mlen)=MKONTO.OR.mMKONTO=SPACE(10));
                           .AND.(GRUPA=MGRUPA.OR.MGRUPA=SPACE(4))
                           
   mfile='OSPOPIS0' 
   uslov1="(MESTO=MMESTO.OR.MMESTO=SPACE(4));
                           .AND.(MTR=MMTR.OR.MMTR=0);
                           .AND.(AG=MAG.OR.MAG=SPACE(1));
                           .AND.(AGPOD=MAGPOD.OR.MAGPOD=SPACE(2))"
   uslov2=".AND.(AGPOD=MAGPOD.OR.MAGPOD=SPACE(2));
                           .AND.(SUBSTR(KONTO,1,mlen)=MKONTO.OR.mMKONTO=SPACE(10));
                           .AND.(GRUPA=MGRUPA.OR.MGRUPA=SPACE(4))"
                           
   USLOV=USLOV1+USLOV2                        
   DO printer_bullzip WITH mdata02,mfile,uslov
                           
ELSE                           
*REPORT FORM OSPOPISBK PREVIEW FOR (MESTO=MMESTO.OR.MMESTO=SPACE(4));
                           .AND.(MTR=MMTR.OR.MMTR=0);
                           .AND.(AG=MAG.OR.MAG=SPACE(1));
                           .AND.(AGPOD=MAGPOD.OR.MAGPOD=SPACE(2));
                           .AND.(KONTO=MKONTO.OR.MKONTO=SPACE(8));
                           .AND.(GRUPA=MGRUPA.OR.MGRUPA=SPACE(4))
                           
   mfile='OSPOPISBK' 
   uslov1="(MESTO=MMESTO.OR.MMESTO=SPACE(4));
                           .AND.(MTR=MMTR.OR.MMTR=0);
                           .AND.(AG=MAG.OR.MAG=SPACE(1));
                           .AND.(AGPOD=MAGPOD.OR.MAGPOD=SPACE(2))"
                           
  USLOV2=".AND.(KONTO=MKONTO.OR.MKONTO=SPACE(8));
                           .AND.(GRUPA=MGRUPA.OR.MGRUPA=SPACE(4))"
  USLOV=USLOV1+USLOV2                           
  DO printer_bullzip WITH mdata02,mfile,uslov

ENDIF
SELECT OSPOPIS
POP KEY