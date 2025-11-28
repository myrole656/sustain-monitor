<?php

namespace App\Notifications;

use Illuminate\Bus\Queueable;
use Illuminate\Notifications\Notification;
use Illuminate\Notifications\Messages\DatabaseMessage;

class ProjectCommentNotification extends Notification
{
    use Queueable;

    protected string $comment;

    public function __construct(string $comment)
    {
        $this->comment = $comment;
    }

    public function via($notifiable): array
    {
        return ['database'];
    }

    public function toDatabase($notifiable): array
    {
        return [
            'message' => $this->comment,
            'from' => auth()->user()->name,
        ];
    }
}
