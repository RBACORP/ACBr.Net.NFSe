﻿// ***********************************************************************
// Assembly         : ACBr.Net.NFSe
// Author           : RFTD
// Created          : 12-08-2016
//
// Last Modified By : RFTD
// Last Modified On : 12-08-2016
// ***********************************************************************
// <copyright file="ProviderABRASFV2.cs" company="ACBr.Net">
//		        		   The MIT License (MIT)
//	     		    Copyright (c) 2016 Grupo ACBr.Net
//
//	 Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//	 The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//	 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary></summary>
// ***********************************************************************

using ACBr.Net.Core.Extensions;
using ACBr.Net.DFe.Core.Serializer;
using ACBr.Net.NFSe.Configuracao;
using ACBr.Net.NFSe.Nota;
using System;
using System.Xml.Linq;

namespace ACBr.Net.NFSe.Providers
{
	// ReSharper disable once InconsistentNaming
	/// <summary>
	/// Class ProviderABRASFV2.
	/// </summary>
	/// <seealso cref="ACBr.Net.NFSe.Providers.ProviderBase" />
	public abstract class ProviderABRASFV2 : ProviderBase
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ProviderABRASFV2"/> class.
		/// </summary>
		/// <param name="config">The configuration.</param>
		/// <param name="municipio">The municipio.</param>
		protected ProviderABRASFV2(ConfiguracoesNFSe config, MunicipioNFSe municipio) : base(config, municipio)
		{
			Name = "ABRASFv2";
		}

		#endregion Constructors

		#region Methods

		#region Load

		public override NotaFiscal LoadXml(XDocument xml)
		{
			throw new NotImplementedException("LoadXml");
		}

		#endregion Load

		#region RPS

		public override string GetXmlRps(NotaFiscal nota, bool identado = true, bool showDeclaration = true)
		{
			var xmlDoc = new XDocument(new XDeclaration("1.0", "UTF-8", null));
			var rootRps = new XElement("Rps");
			xmlDoc.Add(rootRps);

			var infServico = new XElement("InfDeclaracaoPrestacaoServico", new XAttribute("Id", $"{nota.IdentificacaoRps.Numero.OnlyNumbers()}"));
			rootRps.Add(infServico);

			var rps = new XElement("Rps");
			infServico.Add(rps);

			var indRps = new XElement("IdentificacaoRps");
			rps.Add(indRps);

			indRps.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "Numero", 1, 15, Ocorrencia.Obrigatoria, nota.IdentificacaoRps.Numero));
			indRps.AddChild(AdicionarTag(TipoCampo.Str, "", "Serie", 1, 5, Ocorrencia.Obrigatoria, nota.IdentificacaoRps.Serie));
			indRps.AddChild(AdicionarTag(TipoCampo.Int, "", "Tipo", 1, 1, Ocorrencia.Obrigatoria, (int)nota.IdentificacaoRps.Tipo++));

			rps.AddChild(AdicionarTag(TipoCampo.Dat, "", "DataEmissao", 10, 10, Ocorrencia.Obrigatoria, nota.IdentificacaoRps.DataEmissao));
			rps.AddChild(AdicionarTag(TipoCampo.Int, "", "Status", 1, 1, Ocorrencia.Obrigatoria, (int)nota.Situacao++));

			infServico.AddChild(AdicionarTag(TipoCampo.Dat, "", "Competencia", 10, 10, Ocorrencia.Obrigatoria, nota.Competencia));

			var servico = GenerateServicosValoresRps(nota);
			infServico.Add(servico);

			var prestador = GeneratePrestadorRps(nota);
			infServico.Add(prestador);

			if (!nota.Tomador.CpfCnpj.IsEmpty())
			{
				var tomador = GenerateTomadorRps(nota);
				infServico.Add(tomador);
			}

			if (!nota.Intermediario.CpfCnpj.IsEmpty())
			{
				var intermediario = GenerateIntermediarioRps(nota);
				infServico.Add(intermediario);
			}

			if (!nota.ConstrucaoCivil.ArtObra.IsEmpty())
			{
				var construcao = GenerateConstrucaoCivilRps(nota);
				infServico.Add(construcao);
			}

			if (nota.RegimeEspecialTributacao != RegimeEspecialTributacao.Nenhum)
			{
				infServico.AddChild(AdicionarTag(TipoCampo.Int, "", "RegimeEspecialTributacao", 1, 1, Ocorrencia.NaoObrigatoria,
					(int)nota.RegimeEspecialTributacao));
			}

			infServico.AddChild(AdicionarTag(TipoCampo.Int, "", "OptanteSimplesNacional", 1, 1, Ocorrencia.Obrigatoria,
				nota.RegimeEspecialTributacao == RegimeEspecialTributacao.SimplesNacional ? 1 : 2));
			infServico.AddChild(AdicionarTag(TipoCampo.Int, "", "IncentivoFiscal", 1, 1, Ocorrencia.Obrigatoria,
				nota.IncentivadorCultural == NFSeSimNao.Sim ? 1 : 2));

			return xmlDoc.AsString(identado, showDeclaration);
		}

		protected virtual XElement GenerateServicosValoresRps(NotaFiscal nota)
		{
			var servico = new XElement("Servico");

			var valores = new XElement("Valores");
			servico.Add(valores);

			valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorServicos", 1, 15, Ocorrencia.Obrigatoria, nota.Servico.Valores.ValorServicos));
			valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorDeducoes", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorDeducoes));
			valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorPis", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorPis));
			valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorCofins", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorCofins));
			valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorInss", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorInss));
			valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorIr", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorIr));
			valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorCsll", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorCsll));
			valores.AddChild(AdicionarTag(TipoCampo.De2, "", "OutrasRetencoes", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.OutrasRetencoes));
			valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorIss", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorIss));
			valores.AddChild(AdicionarTag(TipoCampo.De2, "", "Aliquota", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.Aliquota));
			valores.AddChild(AdicionarTag(TipoCampo.De2, "", "DescontoIncondicionado", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.DescontoIncondicionado));
			valores.AddChild(AdicionarTag(TipoCampo.De2, "", "DescontoCondicionado", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.DescontoCondicionado));

			servico.AddChild(AdicionarTag(TipoCampo.Int, "", "IssRetido", 1, 1, Ocorrencia.Obrigatoria, (int)nota.Servico.Valores.IssRetido++));
			servico.AddChild(AdicionarTag(TipoCampo.Str, "", "ResponsavelRetencao", 1, 1, Ocorrencia.NaoObrigatoria, nota.Servico.ResponsavelRetencao));
			servico.AddChild(AdicionarTag(TipoCampo.Str, "", "ItemListaServico", 1, 5, Ocorrencia.Obrigatoria, nota.Servico.ItemListaServico));
			servico.AddChild(AdicionarTag(TipoCampo.Str, "", "CodigoCnae", 1, 7, Ocorrencia.NaoObrigatoria, nota.Servico.CodigoCnae));
			servico.AddChild(AdicionarTag(TipoCampo.Str, "", "CodigoTributacaoMunicipio", 1, 20, Ocorrencia.NaoObrigatoria, nota.Servico.CodigoTributacaoMunicipio));
			servico.AddChild(AdicionarTag(TipoCampo.Str, "", "Discriminacao", 1, 2000, Ocorrencia.Obrigatoria, nota.Servico.Discriminacao));
			servico.AddChild(AdicionarTag(TipoCampo.Str, "", "CodigoMunicipio", 1, 20, Ocorrencia.Obrigatoria, nota.Servico.CodigoMunicipio));
			servico.AddChild(AdicionarTag(TipoCampo.Int, "", "CodigoPais", 4, 4, Ocorrencia.NaoObrigatoria, nota.Servico.CodigoPais));
			servico.AddChild(AdicionarTag(TipoCampo.Int, "", "ExigibilidadeISS", 1, 1, Ocorrencia.Obrigatoria, (int)nota.Servico.ExigibilidadeIss++));
			servico.AddChild(AdicionarTag(TipoCampo.Int, "", "MunicipioIncidencia", 7, 7, Ocorrencia.MaiorQueZero, nota.Servico.MunicipioIncidencia));
			servico.AddChild(AdicionarTag(TipoCampo.Str, "", "NumeroProcesso", 0, 30, Ocorrencia.NaoObrigatoria, nota.Servico.NumeroProcesso));

			return servico;
		}

		protected virtual XElement GeneratePrestadorRps(NotaFiscal nota)
		{
			var prestador = new XElement("Prestador");

			var cpfCnpjPrestador = new XElement("CpfCnpj");
			prestador.Add(cpfCnpjPrestador);

			cpfCnpjPrestador.AddChild(AdicionarTagCNPJCPF("", "Cpf", "Cnpj", nota.Prestador.CpfCnpj));

			prestador.AddChild(AdicionarTag(TipoCampo.Str, "", "InscricaoMunicipal", 1, 15, Ocorrencia.NaoObrigatoria, nota.Prestador.InscricaoMunicipal));

			return prestador;
		}

		protected virtual XElement GenerateTomadorRps(NotaFiscal nota)
		{
			var tomador = new XElement("Tomador");

			var ideTomador = new XElement("IdentificacaoTomador");
			tomador.Add(ideTomador);

			var cpfCnpjTomador = new XElement("CpfCnpj");
			ideTomador.Add(cpfCnpjTomador);

			cpfCnpjTomador.AddChild(AdicionarTagCNPJCPF("", "Cpf", "Cnpj", nota.Tomador.CpfCnpj));

			ideTomador.AddChild(AdicionarTag(TipoCampo.Str, "", "InscricaoMunicipal", 1, 15, Ocorrencia.NaoObrigatoria, nota.Tomador.InscricaoMunicipal));

			tomador.AddChild(AdicionarTag(TipoCampo.Str, "", "RazaoSocial", 1, 115, Ocorrencia.NaoObrigatoria, nota.Tomador.RazaoSocial));

			if (!nota.Tomador.Endereco.Logradouro.IsEmpty() ||
				!nota.Tomador.Endereco.Numero.IsEmpty() ||
				!nota.Tomador.Endereco.Complemento.IsEmpty() ||
				!nota.Tomador.Endereco.Bairro.IsEmpty() ||
				nota.Tomador.Endereco.CodigoMunicipio > 0 ||
				!nota.Tomador.Endereco.Uf.IsEmpty() ||
				nota.Tomador.Endereco.CodigoPais > 0 ||
				!nota.Tomador.Endereco.Cep.IsEmpty())
			{
				var endereco = new XElement("Endereco");
				tomador.Add(endereco);

				endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Endereco", 1, 125, Ocorrencia.NaoObrigatoria,
					nota.Tomador.Endereco.Logradouro));
				endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Numero", 1, 10, Ocorrencia.NaoObrigatoria,
					nota.Tomador.Endereco.Numero));
				endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Complemento", 1, 60, Ocorrencia.NaoObrigatoria,
					nota.Tomador.Endereco.Complemento));
				endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Bairro", 1, 60, Ocorrencia.NaoObrigatoria,
					nota.Tomador.Endereco.Bairro));
				endereco.AddChild(AdicionarTag(TipoCampo.Int, "", "CodigoMunicipio", 7, 7, Ocorrencia.MaiorQueZero,
					nota.Tomador.Endereco.CodigoMunicipio));
				endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Uf", 2, 2, Ocorrencia.NaoObrigatoria, nota.Tomador.Endereco.Uf));
				endereco.AddChild(AdicionarTag(TipoCampo.Int, "", "CodigoPais", 4, 4, Ocorrencia.MaiorQueZero,
					nota.Tomador.Endereco.CodigoPais));
				endereco.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "Cep", 8, 8, Ocorrencia.NaoObrigatoria,
					nota.Tomador.Endereco.Cep));
			}

			if (!nota.Tomador.DadosContato.DDD.IsEmpty() ||
				!nota.Tomador.DadosContato.Telefone.IsEmpty() ||
				!nota.Tomador.DadosContato.Email.IsEmpty())
			{
				var contato = new XElement("Contato");
				tomador.Add(contato);

				contato.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "Telefone", 1, 11, Ocorrencia.NaoObrigatoria,
					nota.Tomador.DadosContato.DDD + nota.Tomador.DadosContato.Telefone));
				contato.AddChild(AdicionarTag(TipoCampo.Str, "", "Email", 1, 80, Ocorrencia.NaoObrigatoria,
					nota.Tomador.DadosContato.Email));
			}

			return tomador;
		}

		protected virtual XElement GenerateIntermediarioRps(NotaFiscal nota)
		{
			var intermediario = new XElement("Intermediario");

			var ideIntermediario = new XElement("IdentificacaoIntermediario");
			intermediario.Add(ideIntermediario);

			var cpfCnpjTomador = new XElement("CpfCnpj");
			ideIntermediario.Add(cpfCnpjTomador);

			cpfCnpjTomador.AddChild(AdicionarTagCNPJCPF("", "Cpf", "Cnpj", nota.Intermediario.CpfCnpj));

			ideIntermediario.AddChild(AdicionarTag(TipoCampo.Str, "", "InscricaoMunicipal", 1, 15, Ocorrencia.NaoObrigatoria,
				nota.Intermediario.InscricaoMunicipal));

			intermediario.AddChild(AdicionarTag(TipoCampo.Str, "", "RazaoSocial", 1, 115, Ocorrencia.NaoObrigatoria,
				nota.Intermediario.RazaoSocial));

			return intermediario;
		}

		protected virtual XElement GenerateConstrucaoCivilRps(NotaFiscal nota)
		{
			var construcao = new XElement("ConstrucaoCivil");

			construcao.AddChild(AdicionarTag(TipoCampo.Str, "", "CodigoObra", 1, 15, Ocorrencia.NaoObrigatoria, nota.ConstrucaoCivil.CodigoObra));
			construcao.AddChild(AdicionarTag(TipoCampo.Str, "", "Art", 1, 15, Ocorrencia.Obrigatoria, nota.ConstrucaoCivil.ArtObra));

			return construcao;
		}

		#endregion RPS

		#endregion Methods
	}
}