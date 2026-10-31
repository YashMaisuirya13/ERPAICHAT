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
  conversations: any[] = [];
  activeConversationGuid: string | null = null;
  isLoadingConversations = false;

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
    this.startNewChat();
    this.loadConversations();

    this.route.queryParams.subscribe(params => {
      if (params['prompt']) {
        this.currentInput = params['prompt'];
        setTimeout(() => this.sendMessage(), 400);
      }
    });
  }

  loadConversations(): void {
    this.isLoadingConversations = true;
    this.erpService.getAiConversations().subscribe({
      next: (data: any) => {
        this.conversations = data || [];
        this.isLoadingConversations = false;
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.warn('Failed to load chat history:', err);
        this.isLoadingConversations = false;
        this.cdr.detectChanges();
      }
    });
  }

  selectConversation(guid: string): void {
    if (this.isLoading || this.activeConversationGuid === guid) return;
    this.activeConversationGuid = guid;
    this.isLoading = true;
    this.messages = [];
    this.cdr.detectChanges();

    this.erpService.getAiConversation(guid).subscribe({
      next: (detail: any) => {
        this.isLoading = false;
        if (detail && detail.messages && detail.messages.length > 0) {
          this.messages = detail.messages.map((m: any) => ({
            id: m.messageGuid,
            sender: m.sender,
            text: m.text,
            formattedHtml: m.sender === 'ai' ? this.formatMarkdown(m.text) : this.sanitizer.bypassSecurityTrustHtml(this.escapeHtml(m.text)),
            timestamp: new Date(m.timestamp),
            toolCalled: m.toolCalled,
            dataResult: m.dataResult,
            source: m.sender === 'ai' ? (m.toolCalled ? 'Local ERP Tools' : 'NVIDIA Nemotron') : undefined
          }));
        } else {
          this.startNewChat();
        }
        this.shouldScroll = true;
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.isLoading = false;
        console.error('Failed to load conversation details:', err);
        this.startNewChat();
      }
    });
  }

  startNewChat(): void {
    if (this.isLoading) return;
    this.activeConversationGuid = null;
    this.messages = [];
    this.addAiMessage(
      'Hello! I am your **ERP AI Copilot**, powered by **NVIDIA Nemotron 3 Ultra** and integrated with your live Inventory, Supplier, and Procurement microservices.\n\nHow can I help you manage stock, vendors, or purchase orders today?',
      'NVIDIA-Nemotron-3-Ultra'
    );
    this.shouldScroll = true;
    this.cdr.detectChanges();
  }

  deleteConversation(guid: string, event: Event): void {
    event.stopPropagation();
    if (!confirm('Are you sure you want to delete this conversation?')) return;

    this.erpService.deleteAiConversation(guid).subscribe({
      next: () => {
        this.conversations = this.conversations.filter(c => c.conversationGuid !== guid);
        if (this.activeConversationGuid === guid) {
          this.startNewChat();
        }
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.error('Failed to delete conversation:', err);
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

    let aiMessage: ChatMessage | null = null;

    this.erpService.askAiStream(text, this.activeConversationGuid || undefined).subscribe({
      next: (event: any) => {
        if (event.type === 'start') {
          if (event.conversationGuid) {
            this.activeConversationGuid = event.conversationGuid;
            this.loadConversations();
          }
          if (event.source) {
            this.activeProvider = event.source;
          }
        } else if (event.type === 'tool') {
          if (!aiMessage) {
            this.isLoading = false;
            aiMessage = {
              id: 'ai-' + Date.now(),
              sender: 'ai',
              text: '',
              formattedHtml: this.sanitizer.bypassSecurityTrustHtml(''),
              timestamp: new Date(),
              source: this.activeProvider,
              toolCalled: event.toolCalled,
              dataResult: event.result
            };
            this.messages.push(aiMessage);
          } else {
            aiMessage.toolCalled = event.toolCalled;
            aiMessage.dataResult = event.result;
          }
          this.shouldScroll = true;
          this.cdr.detectChanges();
        } else if (event.type === 'content') {
          if (!aiMessage) {
            this.isLoading = false;
            aiMessage = {
              id: 'ai-' + Date.now(),
              sender: 'ai',
              text: '',
              formattedHtml: this.sanitizer.bypassSecurityTrustHtml(''),
              timestamp: new Date(),
              source: this.activeProvider
            };
            this.messages.push(aiMessage);
          }
          this.isLoading = false;
          aiMessage.text += event.content;
          aiMessage.formattedHtml = this.formatMarkdown(aiMessage.text);
          this.shouldScroll = true;
          this.cdr.detectChanges();
        } else if (event.type === 'done') {
          this.isLoading = false;
          if (event.conversationGuid) {
            this.activeConversationGuid = event.conversationGuid;
            this.loadConversations();
          }
          if (aiMessage) {
            if (event.source) {
              aiMessage.source = event.source;
            }
            if (event.fullText && !aiMessage.text) {
              aiMessage.text = event.fullText;
            }
            aiMessage.formattedHtml = this.formatMarkdown(aiMessage.text);
          } else if (event.fullText) {
            this.addAiMessage(event.fullText, event.source);
          }
          this.shouldScroll = true;
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        console.warn('Streaming failed, executing fallback standard askAi:', err);
        this.erpService.askAi(text, this.activeConversationGuid || undefined).subscribe({
          next: (res: any) => {
            this.isLoading = false;

            if (res?.conversationGuid) {
              this.activeConversationGuid = res.conversationGuid;
              this.loadConversations();
            }

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

            const source = res?.source || (res?.data?.toolCalled ? 'Local ERP Tools' : 'NVIDIA Nemotron');
            const toolCalled = res?.data?.toolCalled;
            const diagnosticNotice = res?.data?.diagnosticNotice;
            const dataResult = res?.data?.result;

            this.addAiMessage(rawResponse, source, toolCalled, diagnosticNotice, dataResult);
            this.shouldScroll = true;
            this.cdr.detectChanges();
          },
          error: (fallbackErr) => {
            this.isLoading = false;
            console.error('AI Copilot request error:', fallbackErr);

            const errorMsg = fallbackErr?.error?.message || fallbackErr?.message || 'Failed to reach AIAgentService.';
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
