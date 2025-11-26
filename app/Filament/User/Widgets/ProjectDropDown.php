<?php

namespace App\Filament\User\Widgets;

use Filament\Widgets\Widget;
use App\Models\Project;
use Illuminate\Support\Facades\Auth;

class ProjectDropDown extends Widget
{
    protected string $view = 'filament.user.widgets.project-dropdown';

    public ?int $projectId = null;

    public function mount(): void
    {
        // Default to the latest project
        $this->projectId = Project::where('user_id', Auth::id())->latest()->value('id');
    }

    public function updatedProjectId(): void
    {
        // Emit event when project changes
        $this->emit('projectChanged', $this->projectId);
    }

    public function getProjectsProperty(): array
    {
        return Project::where('user_id', Auth::id())
            ->pluck('project_name', 'id')
            ->toArray();
    }
}
