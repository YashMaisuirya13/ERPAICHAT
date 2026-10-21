import { Component, ChangeDetectorRef, ViewChild, ElementRef, AfterViewChecked, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { ErpService } from '../../services/erp.service';

export interface ChatMessage {
  id: string;
  sender: 'user' | 'ai';
  text: string;
  formattedHtml?: SafeHtml;
  timestamp: Date;
  source?: string;
  toolCalled?: string;
  diagnosticNotice?: string;
  isError?: boolean;
  dataResult?: any;
}

@Component({
  selector: 'app-ai-assistant',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-assistant.component.html',
  styleUrls: ['./ai-assistant.component.css']
})
export class AiAssistantComponent implements OnInit, AfterViewChecked {
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;

  currentInput = '';
  isLoading = false;
  activeProvider = 'NVIDIA Nemotron 3 Ultra';

  messages: ChatMessage[] = [];

  quickPrompts = [
    { label: '⚠️ Check Low Stock', prompt: 'Check all low stock inventory items and alert me about products below minimum threshold.' },
    { label: '💻 Laptop Stock Level', prompt: 'Check laptop stock levels and show warehouse availability.' },
    { label: '📝 Prepare PO for 20 Laptops', prompt: 'Find suppliers and prepare a purchase order for 20 laptops and submit for approval.' },
    { label: '🤝 Active Suppliers', prompt: 'Show active verified suppliers and their contact details.' },
    { label: '📋 Pending Purchase Orders', prompt: 'List recent purchase orders with their approval status.' },
    { label: '📊 System Status', prompt: 'Summarize overall product inventory and active suppliers.' }
  ];

  private shouldScroll = false;

  constructor(
    private erpService: ErpService,
    private cdr: ChangeDetectorRef,
    private sanitizer: DomSanitizer,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.addAiMessage(
      'Hello! I am your **ERP AI Copilot**, powered by **NVIDIA Nemotron 3 Ultra** and integrated with your live Inventory, Supplier, and Procurement microservices.\n\nHow can I help you manage stock, vendors, or purchase orders today?',
      'NVIDIA-Nemotron-3-Ultra'
    );

    this.route.queryParams.subscribe(params => {
      if (params['prompt']) {
        this.currentInput = params['prompt'];
        setTimeout(() => this.sendMessage(), 400);
      }
    });
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  sendMessage(): void {
    const text = this.currentInput.trim();
    if (!text || this.isLoading) return;

    // Add user message
    this.messages.push({
      id: 'msg-' + Date.now(),
      sender: 'user',
      text,
      formattedHtml: this.sanitizer.bypassSecurityTrustHtml(this.escapeHtml(text)),
      timestamp: new Date()
    });

    this.currentInput = '';
    this.isLoading = true;
    this.shouldScroll = true;
    this.cdr.detectChanges();

    this.erpService.askAi(text).subscribe({
      next: (res: any) => {
        this.isLoading = false;

        let rawResponse = '';
        if (typeof res?.response === 'string') {
          rawResponse = res.response;
        } else if (typeof res?.data?.response === 'string') {
          rawResponse = res.data.response;
        } else if (typeof res?.data === 'string') {
          rawResponse = res.data;
        } else if (res?.message) {
          rawResponse = res.message;
        } else {
          rawResponse = 'I have successfully processed your request.';
        }

        // Clean up accidental raw upstream JSON error payloads
        if (rawResponse.startsWith('{') && rawResponse.includes('"error"')) {
          try {
            const parsed = JSON.parse(rawResponse);
            if (parsed?.error?.message) {
              rawResponse = `⚠️ **Upstream Provider Notice:** ${parsed.error.message}\n\nFalling back to local ERP tools verification.`;
            }
          } catch {
            // Keep original raw response if parse fails
          }
        }

        const source = res?.source || (res?.data?.toolCalled ? 'Local ERP Tools' : 'NVIDIA Nemotron');
        const toolCalled = res?.data?.toolCalled;
        const diagnosticNotice = res?.data?.diagnosticNotice;
        const dataResult = res?.data?.result;

        this.addAiMessage(rawResponse, source, toolCalled, diagnosticNotice, dataResult);
        this.shouldScroll = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        console.error('AI Copilot request error:', err);

        const errorMsg = err?.error?.message || err?.message || 'Failed to reach AIAgentService on port 5048.';
        this.messages.push({
          id: 'err-' + Date.now(),
          sender: 'ai',
          text: `⚠️ **Connection Error**: ${errorMsg}\n\nPlease verify that **AIAgentService** is running and ApiGateway is accessible.`,
          formattedHtml: this.formatMarkdown(`⚠️ **Connection Error**: ${errorMsg}\n\nPlease verify that **AIAgentService** is running and ApiGateway is accessible.`),
          timestamp: new Date(),
          source: 'Error Handler',
          isError: true
        });

        this.shouldScroll = true;
        this.cdr.detectChanges();
      }
    });
  }

  sendPreset(prompt: string): void {
    if (this.isLoading) return;
    this.currentInput = prompt;
    this.sendMessage();
  }

  clearChat(): void {
    this.messages = [];
    this.addAiMessage(
      'Conversation cleared. I am ready for your next ERP query!',
      'NVIDIA-Nemotron-3-Ultra'
    );
    this.shouldScroll = true;
    this.cdr.detectChanges();
  }

  private addAiMessage(
    text: string,
    source?: string,
    toolCalled?: string,
    diagnosticNotice?: string,
    dataResult?: any
  ): void {
    this.messages.push({
      id: 'ai-' + Date.now() + '-' + Math.random().toString(36).substr(2, 4),
      sender: 'ai',
      text,
      formattedHtml: this.formatMarkdown(text),
      timestamp: new Date(),
      source: source || 'NVIDIA-Nemotron',
      toolCalled,
      diagnosticNotice,
      dataResult
    });
  }

  private scrollToBottom(): void {
    try {
      if (this.scrollContainer?.nativeElement) {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
      }
    } catch (err) {
      console.warn('Scroll error:', err);
    }
  }

  private formatMarkdown(raw: string): SafeHtml {
    if (!raw) return this.sanitizer.bypassSecurityTrustHtml('');

    // Extract code blocks first before escaping HTML
    const codeBlocks: string[] = [];
    let text = raw.replace(/```([a-zA-Z0-9_\-\.]*)\r?\n([\s\S]*?)\r?\n```/g, (_, lang, code) => {
      const idx = codeBlocks.length;
      const langLabel = (lang || 'PAYLOAD').toUpperCase();
      const escapedCode = this.escapeHtml(code.trim());
      const blockHtml = `<div class="chat-code-block-wrapper"><div class="code-header"><span class="code-lang">${langLabel}</span></div><pre class="chat-code-block"><code>${escapedCode}</code></pre></div>`;
      codeBlocks.push(blockHtml);
      return `%%CODE_BLOCK_${idx}%%`;
    });

    // Escape HTML for remaining regular text
    text = this.escapeHtml(text);

    // Parse Markdown tables before inline formatting
    text = this.parseMarkdownTables(text);

    // Markdown bold: **text**
    text = text.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');

    // Inline code: `code`
    text = text.replace(/`([^`]+)`/g, '<code class="inline-code">$1</code>');

    // Status badges formatting
    text = text.replace(/⚠️\s*(Low Stock(?: Alert)?)/gi, '<span class="status-badge badge-danger">⚠️ $1</span>');
    text = text.replace(/✅\s*(Healthy|Approved)/gi, '<span class="status-badge badge-success">✅ $1</span>');
    text = text.replace(/🟡\s*(Submitted|Pending)/gi, '<span class="status-badge badge-warning">🟡 $1</span>');
    text = text.replace(/📦\s*(Received)/gi, '<span class="status-badge badge-info">📦 $1</span>');

    // Headers: ### Header
    text = text.replace(/^### (.*$)/gim, '<h4 class="chat-h3">$1</h4>');
    text = text.replace(/^## (.*$)/gim, '<h3 class="chat-h2">$1</h3>');

    // Bullet lists: lines starting with * or -
    text = text.replace(/^\s*[\*\-]\s+(.*)$/gim, '<div class="chat-bullet"><span class="bullet-dot">•</span> <span>$1</span></div>');

    // Numbered lists: lines starting with 1. 2. etc.
    text = text.replace(/^\s*(\d+)\.\s+(.*)$/gim, '<div class="chat-numbered"><span class="num-badge">$1.</span> <span>$2</span></div>');

    // Restore extracted code blocks
    codeBlocks.forEach((block, idx) => {
      text = text.replace(`%%CODE_BLOCK_${idx}%%`, block);
    });

    // Double newlines -> paragraph breaks, single newlines -> <br>
    text = text.replace(/\n\n+/g, '<div class="chat-spacer"></div>');
    text = text.replace(/\n/g, '<br/>');

    return this.sanitizer.bypassSecurityTrustHtml(text);
  }

  private parseMarkdownTables(text: string): string {
    const lines = text.split(/\r?\n/);
    const result: string[] = [];
    let i = 0;

    while (i < lines.length) {
      const line = lines[i].trim();

      // Check if current line is a table header and next line is a separator row
      if (line.startsWith('|') && line.endsWith('|') && i + 1 < lines.length) {
        const nextLine = lines[i + 1].trim();
        const isSeparator = /^\|(\s*:?-+:?\s*\|)+$/.test(nextLine);

        if (isSeparator) {
          const headerCells = line
            .slice(1, -1)
            .split('|')
            .map(c => c.trim());

          const alignments = nextLine
            .slice(1, -1)
            .split('|')
            .map(c => {
              const trimmed = c.trim();
              if (trimmed.startsWith(':') && trimmed.endsWith(':')) return 'center';
              if (trimmed.endsWith(':')) return 'right';
              return 'left';
            });

          i += 2; // Advance past header and separator

          const rows: string[][] = [];
          while (i < lines.length && lines[i].trim().startsWith('|') && lines[i].trim().endsWith('|')) {
            const rowCells = lines[i]
              .trim()
              .slice(1, -1)
              .split('|')
              .map(c => c.trim());
            rows.push(rowCells);
            i++;
          }

          let tableHtml = '<div class="chat-table-wrapper"><table class="chat-table"><thead><tr>';
          headerCells.forEach((h, idx) => {
            const align = alignments[idx] || 'left';
            tableHtml += `<th style="text-align: ${align}">${h}</th>`;
          });
          tableHtml += '</tr></thead><tbody>';

          rows.forEach(r => {
            tableHtml += '<tr>';
            headerCells.forEach((_, idx) => {
              const cell = r[idx] !== undefined ? r[idx] : '';
              const align = alignments[idx] || 'left';
              tableHtml += `<td style="text-align: ${align}">${cell}</td>`;
            });
            tableHtml += '</tr>';
          });

          tableHtml += '</tbody></table></div>';
          result.push(tableHtml);
          continue;
        }
      }

      result.push(lines[i]);
      i++;
    }

    return result.join('\n');
  }

  private escapeHtml(str: string): string {
    return str
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }

  isArray(val: any): boolean {
    return Array.isArray(val);
  }

  hasKeys(val: any): boolean {
    return val && typeof val === 'object' && Object.keys(val).length > 0;
  }
}
