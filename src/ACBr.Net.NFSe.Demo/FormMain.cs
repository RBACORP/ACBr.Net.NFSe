﻿using ACBr.Net.Core.Extensions;
using ACBr.Net.Core.Logging;
using ACBr.Net.DFe.Core.Common;
using ACBr.Net.NFSe.Nota;
using ACBr.Net.NFSe.Providers;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Windows.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ACBr.Net.NFSe.Demo
{
	public partial class FormMain : Form, IACBrLog
	{
		#region Fields

		private ACBrNFSe acbrNFSe;
		private ACBrConfig config;

		#endregion Fields

		#region Constructors

		public FormMain()
		{
			InitializeComponent();
			config = ACBrConfig.CreateOrLoad(Path.Combine(Application.StartupPath, "nfse.config"));
		}

		#endregion Constructors

		#region Methods

		#region EventHandlers

		private void btnSalvarConfig_Click(object sender, EventArgs e)
		{
			SaveConfig();
		}

		private void btnGerarEnviarLoteRps_Click(object sender, EventArgs e)
		{
			ExecuteSafe(() =>
			{
				GerarRps();

				var numero = 10;
				if (InputBox.Show("Numero Lote", "Digite o numero do lote.", ref numero).Equals(DialogResult.Cancel)) return;

				var ret = acbrNFSe.Enviar(numero);
				wbbDados.LoadXml(ret.XmlEnvio);
				wbbResposta.LoadXml(ret.XmlRetorno);
			});
		}

		private void btnConsultarSituacao_Click(object sender, EventArgs e)
		{
			ExecuteSafe(() =>
			{
				var numero = 10;
				if (InputBox.Show("Numero Lote", "Digite o numero do lote.", ref numero).Equals(DialogResult.Cancel)) return;

				var protocolo = "0";
				if (InputBox.Show("Numero do Protocolo", "Digite o numero do protocolo.", ref protocolo).Equals(DialogResult.Cancel)) return;

				var ret = acbrNFSe.ConsultarSituacao(numero, protocolo);
				wbbDados.LoadXml(ret.XmlEnvio);
				wbbResposta.LoadXml(ret.XmlRetorno);
			});
		}

		private void btnConsultarLote_Click(object sender, EventArgs e)
		{
			ExecuteSafe(() =>
			{
				var numero = 10;
				InputBox.Show("Numero Lote", "Digite o numero do lote.", ref numero);

				var protocolo = "0";
				InputBox.Show("Numero do Protocolo", "Digite o numero do protocolo.", ref protocolo);

				var ret = acbrNFSe.ConsultarLoteRps(numero, protocolo);
				wbbDados.LoadXml(ret.XmlEnvio);
				wbbResposta.LoadXml(ret.XmlRetorno);
			});
		}

		private void btnConsultarNFSeRps_Click(object sender, EventArgs e)
		{
			ExecuteSafe(() =>
			{
				var numero = "10";
				InputBox.Show("Numero da RPS", "Digite o numero da RPS.", ref numero);

				var serie = "0";
				InputBox.Show("Serie da RPS", "Digite o numero da serie da RPS.", ref serie);

				var ret = acbrNFSe.ConsultaNFSeRps(numero, serie, TipoRps.RPS);
				wbbDados.LoadXml(ret.XmlEnvio);
				wbbResposta.LoadXml(ret.XmlRetorno);
			});
		}

		private void btnSelecionarSchema_Click(object sender, EventArgs e)
		{
			ExecuteSafe(() =>
			{
				txtSchemas.Text = Helpers.SelectFolder();
			});
		}

		private void btnPathXml_Click(object sender, EventArgs e)
		{
			ExecuteSafe(() =>
			{
				txtPathXml.Text = Helpers.SelectFolder();
			});
		}

		private void btnSelecionarArquivo_Click(object sender, EventArgs e)
		{
			LoadMunicipios();
		}

		private void btnAdicionar_Click(object sender, EventArgs e)
		{
			ExecuteSafe(() =>
			{
				var municipio = new MunicipioNFSe();
				if (FormEdtMunicipio.Editar(municipio).Equals(DialogResult.Cancel)) return;

				AddMunicipio(municipio);
			});
		}

		private void btnDeletar_Click(object sender, EventArgs e)
		{
			ExecuteSafe(() =>
			{
				if (lstMunicipios.SelectedItems.Count < 1) return;

				if (MessageBox.Show(@"Você tem certeza?", @"ACBrNFSe Demo", MessageBoxButtons.YesNo).Equals(DialogResult.No)) return;

				var municipio = lstMunicipios.SelectedItems[0];
				lstMunicipios.Items.Remove(municipio);
				UpdateCidades();
			});
		}

		private void btnCarregar_Click(object sender, EventArgs e)
		{
			LoadMunicipios();
		}

		private void btnSalvar_Click(object sender, EventArgs e)
		{
			ExecuteSafe(() =>
			{
				if (lstMunicipios.Items.Count < 1) return;

				var path = Helpers.SelectFolder();
				if (path.IsEmpty()) return;

				var municipios = lstMunicipios.Items.Cast<ListViewItem>().Select(x => (MunicipioNFSe)x.Tag);
				ProviderManager.Municipios.Clear();
				ProviderManager.Municipios.AddRange(municipios);
				ProviderManager.Save(Path.Combine(path, "Municipios.nfse"));
			});
		}

		private void btnGetCertificate_Click(object sender, EventArgs e)
		{
			ExecuteSafe(() =>
			{
				var numeroSerie = acbrNFSe.Configuracoes.Certificados.SelecionarCertificado();
				txtNumeroSerie.Text = numeroSerie;
			});
		}

		private void btnFindCertificate_Click(object sender, EventArgs e)
		{
			ExecuteSafe(() =>
			{
				var file = Helpers.OpenFile("Certificate Files (*.pfx)|*.pfx|All Files (*.*)|*.*", "Selecione o certificado");
				txtCertificado.Text = file;
			});
		}

		private void lstMunicipios_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			ExecuteSafe(() =>
			{
				if (lstMunicipios.SelectedItems.Count < 1) return;

				var municipio = lstMunicipios.SelectedItems[0].Tag as MunicipioNFSe;
				if (FormEdtMunicipio.Editar(municipio).Equals(DialogResult.Cancel)) return;

				LoadData();
			});
		}

		#endregion EventHandlers

		#region ValueChanged

		private void txtCNPJ_TextChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.PrestadorPadrao.CpfCnpj = txtCPFCNPJ.Text.OnlyNumbers();
		}

		private void txtIM_TextChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.PrestadorPadrao.InscricaoMunicipal = txtIM.Text.OnlyNumbers();
		}

		private void txtRazaoSocial_TextChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.PrestadorPadrao.RazaoSocial = txtRazaoSocial.Text;
		}

		private void txtFantasia_TextChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.PrestadorPadrao.NomeFantasia = txtFantasia.Text;
		}

		private void txtFone_TextChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.PrestadorPadrao.DadosContato.Telefone = txtFone.Text;
		}

		private void txtCEP_TextChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.PrestadorPadrao.Endereco.Cep = txtCEP.Text;
		}

		private void txtEndereco_TextChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.PrestadorPadrao.Endereco.Logradouro = txtEndereco.Text;
		}

		private void txtNumero_TextChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.PrestadorPadrao.Endereco.Numero = txtNumero.Text;
		}

		private void txtComplemento_TextChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.PrestadorPadrao.Endereco.Complemento = txtComplemento.Text;
		}

		private void txtBairro_TextChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.PrestadorPadrao.Endereco.Bairro = txtBairro.Text;
		}

		private void cmbCidades_SelectedValueChanged(object sender, EventArgs e)
		{
			var municipio = (MunicipioNFSe)cmbCidades.SelectedItem;
			if (municipio == null) return;

			txtUf.Text = municipio.UF;
			txtCodCidade.Text = municipio.Codigo.ToString();
			txtCodSiafi.Text = municipio.CodigoSiafi.ToString();

			acbrNFSe.Configuracoes.WebServices.CodigoMunicipio = municipio.Codigo;
			acbrNFSe.Configuracoes.PrestadorPadrao.Endereco.Municipio = municipio.Nome;
			acbrNFSe.Configuracoes.PrestadorPadrao.Endereco.CodigoMunicipio = municipio.Codigo;
			acbrNFSe.Configuracoes.PrestadorPadrao.Endereco.Uf = municipio.UF;
		}

		private void txtCertificado_TextChanged(object sender, EventArgs e)
		{
			if (txtNumeroSerie.Text.IsEmpty()) return;

			txtNumeroSerie.Text = string.Empty;
			acbrNFSe.Configuracoes.Certificados.Certificado = txtCertificado.Text;
		}

		private void txtSenha_TextChanged(object sender, EventArgs e)
		{
			if (txtSenha.Text.IsEmpty()) return;

			acbrNFSe.Configuracoes.Certificados.Senha = txtSenha.Text;
		}

		private void txtNumeroSerie_TextChanged(object sender, EventArgs e)
		{
			if (txtNumeroSerie.Text.IsEmpty()) return;

			txtCertificado.Text = string.Empty;
			txtSenha.Text = string.Empty;
			acbrNFSe.Configuracoes.Certificados.Certificado = txtNumeroSerie.Text;
			acbrNFSe.Configuracoes.Certificados.Senha = string.Empty;
		}

		private void txtSchemas_TextChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.Geral.PathSchemas = txtSchemas.Text;
		}

		private void chkSalvarArquivos_CheckedChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.Geral.Salvar = chkSalvarArquivos.Checked;
		}

		private void txtArquivoCidades_Click(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.Geral.ArquivoMunicipios = txtArquivoCidades.Text;
		}

		private void txtPathXml_TextChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.Geral.PathSalvar = txtPathXml.Text;
		}

		private void txtArquivoCidades_TextChanged(object sender, EventArgs e)
		{
		}

		private void cmbAmbiente_SelectedValueChanged(object sender, EventArgs e)
		{
			acbrNFSe.Configuracoes.WebServices.Ambiente = (DFeTipoAmbiente)cmbAmbiente.SelectedItem;
		}

		#endregion ValueChanged

		#region Overrides

		protected override void OnShown(EventArgs e)
		{
			acbrNFSe = new ACBrNFSe();
			acbrNFSe.Configuracoes.Geral.RetirarAcentos = true;

			InitializeLog();
			this.Log().Debug("Log Iniciado");

			cmbAmbiente.EnumDataSource<DFeTipoAmbiente>(DFeTipoAmbiente.Homologacao);
			LoadData();
			LoadConfig();
		}

		#endregion Overrides

		private void GerarRps()
		{
			var municipio = (MunicipioNFSe)cmbCidades.SelectedItem;
			if (municipio == null) return;

			acbrNFSe.NotasFiscais.Clear();
			var nfSe = acbrNFSe.NotasFiscais.AddNew();
			nfSe.IdentificacaoRps.Numero = "1";
			nfSe.IdentificacaoRps.Serie = "1";
			nfSe.IdentificacaoRps.Tipo = TipoRps.RPS;
			nfSe.IdentificacaoRps.DataEmissao = DateTime.Now;
			nfSe.Situacao = SituacaoNFSeRps.Normal;
			nfSe.NaturezaOperacao = NaturezaOperacao.NT01;
			nfSe.RegimeEspecialTributacao = RegimeEspecialTributacao.SimplesNacional;
			nfSe.IncentivadorCultural = NFSeSimNao.Nao;

			nfSe.Servico.ItemListaServico = "107";
			nfSe.Servico.CodigoTributacaoMunicipio = "01.07.00 / 00010700";
			nfSe.Servico.CodigoCnae = "";
			nfSe.Servico.CodigoMunicipio = municipio.Codigo;
			nfSe.Servico.Discriminacao = "MANUTENCAO TÉCNICA / VOCÊ PAGOU APROXIMADAMENTE R$ 41,15 DE TRIBUTOS FEDERAIS, R$ 8,26 DE TRIBUTOS MUNICIPAIS, R$ 256,57 PELOS PRODUTOS/SERVICOS, FONTE: IBPT.";

			nfSe.Servico.Valores.ValorServicos = 100;
			nfSe.Servico.Valores.ValorDeducoes = 0;
			nfSe.Servico.Valores.ValorPis = 0;
			nfSe.Servico.Valores.ValorCofins = 0;
			nfSe.Servico.Valores.ValorInss = 0;
			nfSe.Servico.Valores.ValorIr = 0;
			nfSe.Servico.Valores.ValorCsll = 0;
			nfSe.Servico.Valores.IssRetido = SituacaoTributaria.Normal;
			nfSe.Servico.Valores.ValorIss = 0;
			nfSe.Servico.Valores.ValorOutrasRetencoes = 0;
			nfSe.Servico.Valores.BaseCalculo = 100;
			nfSe.Servico.Valores.Aliquota = 2;
			nfSe.Servico.Valores.ValorLiquidoNfse = 0;
			nfSe.Servico.Valores.ValorIssRetido = 0;
			nfSe.Servico.Valores.DescontoCondicionado = 0;
			nfSe.Servico.Valores.DescontoIncondicionado = 0;
			nfSe.ValorCredito = 0;

			nfSe.Tomador.CpfCnpj = "44854962283";
			nfSe.Tomador.InscricaoMunicipal = "";
			nfSe.Tomador.RazaoSocial = "Nome";

			nfSe.Tomador.Endereco.TipoLogradouro = "";
			nfSe.Tomador.Endereco.Logradouro = "INDEPENDENCIA";
			nfSe.Tomador.Endereco.Numero = "123";
			nfSe.Tomador.Endereco.Complemento = "SL 10";
			nfSe.Tomador.Endereco.Bairro = "VILA SEIXAS";
			nfSe.Tomador.Endereco.CodigoMunicipio = municipio.Codigo;
			nfSe.Tomador.Endereco.Municipio = municipio.Nome;
			nfSe.Tomador.Endereco.Uf = municipio.UF;
			nfSe.Tomador.Endereco.Cep = "14020010";
			nfSe.Tomador.Endereco.CodigoPais = 1058;
			nfSe.Tomador.Endereco.Pais = "BRASIL";

			nfSe.Tomador.DadosContato.DDD = "16";
			nfSe.Tomador.DadosContato.Telefone = "30111234";
			nfSe.Tomador.DadosContato.Email = "NOME@EMPRESA.COM.BR";
		}

		private void AddMunicipio(params MunicipioNFSe[] municipios)
		{
			ProviderManager.Municipios.AddRange(municipios);
			LoadData();
		}

		private void LoadData()
		{
			var itens = new List<ListViewItem>();

			foreach (var municipio in ProviderManager.Municipios)
			{
				var item = new ListViewItem(municipio.Nome);
				item.SubItems.Add(municipio.UF);
				item.SubItems.Add(municipio.Codigo.ToString());
				item.SubItems.Add(municipio.CodigoSiafi.ToString());
				item.SubItems.Add(municipio.Provedor);
				item.Tag = municipio;

				itens.Add(item);
			}

			lstMunicipios.BeginUpdate();

			lstMunicipios.Items.Clear();
			lstMunicipios.Items.AddRange(itens.ToArray());

			lstMunicipios.EndUpdate();

			UpdateCidades();
		}

		private void LoadMunicipios()
		{
			ExecuteSafe(() =>
			{
				var arquivo = Helpers.OpenFile("Arquivo de cidades NFSe (*.nfse) | *.nfse |Todos os arquivos | *.*", "Selecione o arquivo de cidades");
				if (arquivo.IsEmpty()) return;

				ProviderManager.Load(arquivo);
				txtArquivoCidades.Text = arquivo;
				LoadData();
			});
		}

		private void UpdateCidades()
		{
			cmbCidades.DataSource = null;
			cmbCidades.Items.Clear();
			cmbCidades.DisplayMember = "Nome";
			cmbCidades.ValueMember = "Codigo";
			cmbCidades.DataSource = ProviderManager.Municipios;
		}

		private void InitializeLog()
		{
			var config = new LoggingConfiguration();
			var target = new RichTextBoxTarget
			{
				UseDefaultRowColoringRules = true,
				Layout = @"${date:format=dd/MM/yyyy HH\:mm\:ss} - ${message}",
				FormName = Name,
				ControlName = rtbLog.Name,
				AutoScroll = true
			};

			config.AddTarget("RichTextBox", target);
			config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, target));

			var infoTarget = new FileTarget
			{
				FileName = "${basedir:dir=Logs:file=ACBrNFSe.log}",
				Layout = "${processid}|${longdate}|${level:uppercase=true}|" +
						 "${event-context:item=Context}|${logger}|${message}",
				CreateDirs = true,
				Encoding = Encoding.UTF8,
				MaxArchiveFiles = 93,
				ArchiveEvery = FileArchivePeriod.Day,
				ArchiveNumbering = ArchiveNumberingMode.Date,
				ArchiveFileName = "${basedir}/Logs/Archive/${date:format=yyyy}/${date:format=MM}/ACBrNFSe_{{#}}.log",
				ArchiveDateFormat = "dd.MM.yyyy"
			};

			config.AddTarget("infoFile", infoTarget);
			config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, infoTarget));
			LogManager.Configuration = config;
		}

		private void LoadConfig()
		{
			var cnpj = config.Get("PrestadorCPFCNPJ", string.Empty);
			if (!cnpj.IsEmpty())
			{
				txtCPFCNPJ.Text = cnpj.FormataCPFCNPJ();
			}

			txtIM.Text = config.Get("PrestadorIM", string.Empty);
			txtRazaoSocial.Text = config.Get("PrestadorRazaoSocial", string.Empty);
			txtFantasia.Text = config.Get("PrestadorFantasia", string.Empty);
			txtFone.Text = config.Get("PrestadorFone", string.Empty);
			txtCEP.Text = config.Get("PrestadorCEP", string.Empty);
			txtEndereco.Text = config.Get("PrestadorEndereco", string.Empty);
			txtNumero.Text = config.Get("PrestadorNumero", string.Empty);
			txtComplemento.Text = config.Get("PrestadorComplemento", string.Empty);
			txtBairro.Text = config.Get("PrestadorBairro", string.Empty);

			var codMunicipio = config.Get("Municipio", 0);
			if (codMunicipio > 0)
			{
				var municipio = ProviderManager.Municipios.SingleOrDefault(x => x.Codigo == codMunicipio);
				if (municipio != null)
				{
					cmbCidades.SelectedItem = municipio;
				}
			}

			cmbAmbiente.SelectedItem = config.Get("Ambiente", DFeTipoAmbiente.Homologacao);

			txtCertificado.Text = config.Get("Certificado", string.Empty);
			txtSenha.Text = config.Get("Senha", string.Empty);
			txtNumeroSerie.Text = config.Get("NumeroSerie", string.Empty);
		}

		private void SaveConfig()
		{
			config.Set("PrestadorCPFCNPJ", txtCPFCNPJ.Text.OnlyNumbers());
			config.Set("PrestadorIM", txtIM.Text.OnlyNumbers());
			config.Set("PrestadorRazaoSocial", txtRazaoSocial.Text);
			config.Set("PrestadorFantasia", txtFantasia.Text);
			config.Set("PrestadorFone", txtFone.Text);
			config.Set("PrestadorCEP", txtCEP.Text);
			config.Set("PrestadorEndereco", txtEndereco.Text);
			config.Set("PrestadorNumero", txtNumero.Text);
			config.Set("PrestadorComplemento", txtComplemento.Text);
			config.Set("PrestadorBairro", txtBairro.Text);

			config.Set("Municipio", txtCodCidade.Text.OnlyNumbers());

			config.Set("Ambiente", (DFeTipoAmbiente)cmbAmbiente.SelectedItem);

			config.Set("Certificado", txtCertificado.Text);
			config.Set("Senha", txtSenha.Text);
			config.Set("NumeroSerie", txtNumeroSerie.Text);

			config.Save();
		}

		private void ExecuteSafe(Action action)
		{
			try
			{
				action();
			}
			catch (Exception exception)
			{
				lblStatus.Text = exception.Message;
			}
		}

		#endregion Methods
	}
}