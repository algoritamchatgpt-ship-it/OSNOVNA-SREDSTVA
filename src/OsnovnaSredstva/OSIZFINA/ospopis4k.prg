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
USE FIRMA IN 0
SELECT OS

*REPORT FORM OSPOPIS04 PREVIEW FOR (MESTO=MMESTO.OR.MMESTO=SPACE(4));
                           .AND.(MTR=MMTR.OR.MMTR=0);
                           .AND.(AG=MAG.OR.MAG=SPACE(1));
                           .AND.(AGPOD=MAGPOD.OR.MAGPOD=SPACE(2));
                           .AND.(SUBSTR(KONTO,1,mlen)=MKONTO.OR.mMKONTO=SPACE(10));
                           .AND.(GRUPA=MGRUPA.OR.MGRUPA=SPACE(4))
                           
   mfile='OSPOPIS04' 
   uslov1="(MESTO=MMESTO.OR.MMESTO=SPACE(4));
                           .AND.(MTR=MMTR.OR.MMTR=0);
                           .AND.(AG=MAG.OR.MAG=SPACE(1));
                           .AND.(AGPOD=MAGPOD.OR.MAGPOD=SPACE(2))"
   uslov2=".AND.(SUBSTR(KONTO,1,mlen)=MKONTO.OR.mMKONTO=SPACE(10));
                           .AND.(GRUPA=MGRUPA.OR.MGRUPA=SPACE(4))"
   uslov=uslov1+uslov2                           
   DO printer_bullzip WITH mdata02,mfile,uslov
                           
                           
                           
SELECT FIRMA
USE
SELECT OSPOPIS
POP KEY