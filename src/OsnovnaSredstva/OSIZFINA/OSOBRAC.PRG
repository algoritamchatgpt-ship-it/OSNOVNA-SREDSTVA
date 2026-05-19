Push Key Clear
Go Top

*Suspend

Do While.Not.Eof()

	Replace SAD0 With NAB0-ISP0
	Replace SAD02 With NAB02-ISP02

*-------Stara osnovna sredstva poreski obracun amortizacije
	If datstartam < Ctod('01/01/2019')
		If NAB02<>0
			Replace SAD02 With NAB02-ISP02
			If AG='1'
				MAMORT=Round(NAB02*STOPAOT2*BRMES/(12*100),2)
			Else
				MAMORT=Round(SAD02*STOPAOT2*BRMES/(12*100),2)
			Endif
			If Abs(MAMORT)>Abs(NAB02)-Abs(ISP02)
				MMAMORT=SAD02
			Else
				MMAMORT=MAMORT
			Endif
			Replace AMORT2 With MMAMORT
		Else
			Replace AMORT2 With 0
		Endif
		If Abs(ISP02)+Abs(AMORT2)>Abs(NAB02)
			Replace AMORT2 With NAB02-ISP02
		Endif
		Replace NAB2 With NAB02
		Replace ISP2 With ISP02+AMORT2
		Replace SAD2 With NAB2-ISP2
*-------Stara osnovna sredstva racunovodstveni (MRS) obracun amortizacije
		If NAB0<>0
			If PROCGOD<>0
				MAMORT=Round((SAD0)*STOPAOT*BRMES/(12*PROCGOD*100),2)
			Else
				MAMORT=Round((NAB0)*STOPAOT*BRMES/(12*100),2)
			Endif
			If Abs(MAMORT)>Abs(NAB0)-Abs(ISP0)
				MMAMORT=SAD0
			Else
				MMAMORT=MAMORT
			Endif
			Replace AMORT With MMAMORT
		Else
			Replace AMORT With 0
		Endif
		If Abs(ISP0)+Abs(AMORT)>Abs(NAB0)
			Replace AMORT With NAB0-ISP0
		Endif
		Replace NAB With NAB0
		Replace ISP With ISP0+AMORT
		Replace SAD With NAB-ISP
	Endif
*-------Nova osnovna sredstva racunovodstveni (MRS) obracun amortizacije
*-------Samo za nacinob ='014' nematerijalna ulaganja
	If  datstartam >= Ctod('01/01/2019') And nacinob ='014'
		If NAB0<>0
			If PROCGOD<>0
				MAMORT=Round((NAB0)*STOPAOT*BRMES/(12*PROCGOD*100),2) && bilo (sad0)
			Else
				MAMORT=Round((NAB0)*STOPAOT*BRMES/(12*100),2)
			Endif
			If Abs(MAMORT)>Abs(NAB0)-Abs(ISP0)
				MMAMORT=SAD0 	&& bilo sad0
			Else
				MMAMORT=MAMORT
			Endif
			Replace AMORT With MMAMORT
		Else
			Replace AMORT With 0
		Endif
		If Abs(ISP0)+Abs(AMORT)>Abs(NAB0)
			Replace AMORT With NAB0-ISP0
		Endif
		Replace NAB With NAB0
		Replace ISP With ISP0+AMORT
		Replace SAD With NAB-ISP
	Endif
*-------Nova osnovna sredstva poreski obracun amortizacije sa uvecanjem iznosulag ako ga ima inace je nula
	If  datstartam >= Ctod('01/01/2019') And Upper(nacinob) ='POA'
*---- Za nova os sredstva ako je racunovodstvena vrednost 0 ne racuna poresku amort.
		If SAD0 # 0
			If NAB02<>0
				Replace AMORT2 With 0
				Replace AMORT With 0
				Replace SAD02 With NAB02-ISP02
*------ ako je bilo ulaganja uvecati nab02 za iznos ulaganja
				If Between(datulag,datum0,datum1)
					wnab02=NAB02+IZNOSULAG
				Else
					wnab02=NAB02
				Endif
				*MAMORT=Round((wnab02)*STOPAOT2*BRMES/(12*100),2)
*----- racuna poresku za nova os.sredstva preko broja dana umesto broja meseci
				IF DAY(datum0)# 01
					a=01
					b=MONTH(datum0)+1
					c=YEAR(datum0)
					d=CTOD(STR(a)+str(b)+STR(c))
					wbrmes= datum1-d+1
				ELSE
					wbrmes= datum1-datum0+1
				endif
				IF brmes =0
					wbrmes=0
				ENDIF
				
				replace polje1 WITH wbrmes
					
				MAMORT=Round((wnab02)*STOPAOT2*wbrmes/(365*100),2)
				MMAMORT=MAMORT
				Replace AMORT2 With MMAMORT
			Else
				Replace AMORT2 With 0
			Endif
			If Abs(ISP02)+Abs(AMORT2)>Abs(wnab02)
				Replace AMORT2 With (wnab02)-ISP02
			Endif
*---uvecana NAB2 za ulaganja ako ih je bilo u obr.periodu
			Replace NAB2 With wnab02
		Else

		Endif
*-------Nova osnovna sredstva racunovodstveni (MRS) obracun amortizacije
*---- Dodatak ako je bilo novih ulaganja za obracunski period
		If Between(datulag,datum0,datum1)
*------ ako je bilo ulaganja uvecati nab0 za iznos ulaganja
			wbrmes =(Month(datum1)- Month(datulag))+1
			wnab0=NAB0+IZNOSULAG
*--- racunanje amortizacije za iznos ulaganja od datuma ulaganja do kraja obr.perioda
			wAMORT=Round((IZNOSULAG)*STOPAOT*wbrmes/(12*100),2)
		Else
			wnab0=NAB0
			wAMORT=0
		Endif
*---- Kraj dodatka ako je bilo novih ulaganja za obracunski period
		If wnab0<>0
			If PROCGOD<>0
				MAMORT=Round((wnab0-IZNOSULAG)*STOPAOT*BRMES/(12*PROCGOD*100),2) && bilo (sad0)
			Else
				MAMORT=Round((wnab0-IZNOSULAG)*STOPAOT*BRMES/(12*100),2)
			Endif
*--- uvecava amotizacuju za amortizaciju dodatnog ulaganja.
			MAMORT=MAMORT+wAMORT

			If Abs(MAMORT)>Abs(wnab0)-Abs(ISP0)
				MMAMORT=SAD0		&& bilo sad0
			Else
				MMAMORT=MAMORT
			Endif
			Replace AMORT With MMAMORT
		Else
			Replace AMORT With 0
		Endif
		If Abs(ISP0)+Abs(AMORT)>Abs(wnab0)
			Replace AMORT With wnab0-ISP0
		Endif
*--------- ubacen if jer treba za poresku racunati manju amortizaciju amor por/amor rac
*--------- IZMENUTI ako je amort=0 ali ne razumem sta ???
		If AMORT<AMORT2
			Replace ISP2 With ISP02+AMORT && AMORT2
			Replace SAD2 With NAB2-ISP2
		Else
			Replace ISP2 With ISP02+AMORT2
			If SAD0 # 0
				Replace SAD2 With NAB2-ISP2
			Else
				Replace SAD2 With SAD02
			Endif
		Endif
		Replace NAB With wnab0
		Replace ISP With ISP0+AMORT
		Replace SAD With NAB-ISP
	Endif

********* za nova os sredstva upisuje koja je manja amortizacija  AND datstartam >= Ctod('01/01/2019')

	If datstartam >= Ctod('01/01/2019') &&!Empty(nacinob) && And Upper(nacinob)='POA'

		If nacinob='014'
			Replace ram With AMORT
		Else
			If AMORT>AMORT2 Or AMORT=AMORT2
				Replace pam With AMORT2
			Else
				Replace ram With AMORT
			Endif
		Endif
	Endif

	Skip
Enddo
Pop Key
