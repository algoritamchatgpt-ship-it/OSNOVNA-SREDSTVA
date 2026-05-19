Push Key Clear
Go Top

*suspend

Do While.Not.Eof()
	Replace SAD0 With NAB0-ISP0
	Replace SAD02 With NAB02-ISP02
	Do Case
	Case  datstartam < Ctod('01/01/2019')
** obracun za stara osnovna sredstva
*----------PREMA PORESKIM PROPISIMA
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
**   racunovodstveni obracun za stara osnovna sredstva
**	Case Empty(nacinob)
*----------PREMA M R STANDARDIMA
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




**obracun za nova os sredstva

	Case  datstartam >= Ctod('01/01/2019')
		If Upper(nacinob) ='POA'
			If NAB02<>0
				Replace SAD02 With NAB02-ISP02

				MAMORT=Round(NAB02*STOPAOT2*BRMES/(12*100),2)

				MMAMORT=MAMORT

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
		Endif
**   racunovodstveni obracun za nova (POA/014) osnovna sredstva
**Case !Empty(nacinob)
		If NAB0<>0
			If PROCGOD<>0
				MAMORT=Round((NAB0)*STOPAOT*BRMES/(12*PROCGOD*100),2) && bilo (sad0)
			Else
				MAMORT=Round((NAB0)*STOPAOT*BRMES/(12*100),2)
			Endif
			If Abs(MAMORT)>Abs(NAB0)-Abs(ISP0)
				MMAMORT=NAB0		&& bilo sad0
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
		
** obracun za nova os nematerijalna ulaganja
	Case  datstartam >= Ctod('01/01/2019') And nacinob ='014'
		Wait Window 'Nacin obracuna 014 nije jos uradjen za Sifru '+ osifra



	Otherwise
		Wait Window 'Nesto nije u redu'
	Endcase

********* za nova os sredstva upisuje koja je manja amortizacija
	If !Empty(nacinob)
		If AMORT>AMORT2 Or AMORT=AMORT2
			Replace pam With AMORT2
		Else
			Replace ram With AMORT
		Endif
	Endif

	Skip
Enddo
Pop Key
